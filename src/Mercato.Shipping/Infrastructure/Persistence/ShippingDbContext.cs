using Mercato.Shipping.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Shipping.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Shipping module.
/// </summary>
public class ShippingDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the shipping providers.
    /// </summary>
    public DbSet<ShippingProvider> ShippingProviders { get; set; } = null!;

    /// <summary>
    /// Gets or sets the store shipping providers.
    /// </summary>
    public DbSet<StoreShippingProvider> StoreShippingProviders { get; set; } = null!;

    /// <summary>
    /// Gets or sets the shipments.
    /// </summary>
    public DbSet<Shipment> Shipments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the shipment status updates.
    /// </summary>
    public DbSet<ShipmentStatusUpdate> ShipmentStatusUpdates { get; set; } = null!;

    /// <summary>
    /// Gets or sets the shipping labels.
    /// </summary>
    public DbSet<ShippingLabel> ShippingLabels { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ShippingProvider entity
        modelBuilder.Entity<ShippingProvider>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.ApiEndpointUrl)
                .HasMaxLength(500);

            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500);

            entity.Property(e => e.TrackingUrlTemplate)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // Configure StoreShippingProvider entity
        modelBuilder.Entity<StoreShippingProvider>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CredentialIdentifier)
                .HasMaxLength(200);

            entity.Property(e => e.AccountNumber)
                .HasMaxLength(100);

            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => new { e.StoreId, e.ShippingProviderId }).IsUnique();

            entity.HasOne(e => e.ShippingProvider)
                .WithMany()
                .HasForeignKey(e => e.ShippingProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Shipment entity
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ExternalShipmentId)
                .HasMaxLength(200);

            entity.Property(e => e.StatusMessage)
                .HasMaxLength(500);

            entity.Property(e => e.LabelUrl)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.SellerSubOrderId);
            entity.HasIndex(e => e.StoreShippingProviderId);
            entity.HasIndex(e => e.TrackingNumber);
            entity.HasIndex(e => e.ExternalShipmentId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.StoreShippingProvider)
                .WithMany()
                .HasForeignKey(e => e.StoreShippingProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.StatusUpdates)
                .WithOne(u => u.Shipment)
                .HasForeignKey(u => u.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ShipmentStatusUpdate entity
        modelBuilder.Entity<ShipmentStatusUpdate>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.StatusMessage)
                .HasMaxLength(500);

            entity.Property(e => e.Location)
                .HasMaxLength(200);

            entity.HasIndex(e => e.ShipmentId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure ShippingLabel entity
        modelBuilder.Entity<ShippingLabel>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.LabelFormat)
                .HasMaxLength(20);

            entity.HasIndex(e => e.ShipmentId).IsUnique();

            entity.HasOne(e => e.Shipment)
                .WithMany()
                .HasForeignKey(e => e.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
