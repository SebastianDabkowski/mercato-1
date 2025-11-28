using Mercato.Buyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Buyer.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Buyer module.
/// </summary>
public class BuyerDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuyerDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public BuyerDbContext(DbContextOptions<BuyerDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the delivery addresses DbSet.
    /// </summary>
    public DbSet<DeliveryAddress> DeliveryAddresses { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDeliveryAddress(modelBuilder);
    }

    private static void ConfigureDeliveryAddress(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryAddress>(entity =>
        {
            entity.ToTable("DeliveryAddresses");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.BuyerId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.Label)
                .HasMaxLength(50);

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.AddressLine1)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.AddressLine2)
                .HasMaxLength(500);

            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.State)
                .HasMaxLength(100);

            entity.Property(e => e.PostalCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Country)
                .IsRequired()
                .HasMaxLength(2);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(e => e.IsDefault)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdatedAt)
                .IsRequired();

            // Index for querying addresses by buyer
            entity.HasIndex(e => e.BuyerId);

            // Index for querying default address by buyer
            entity.HasIndex(e => new { e.BuyerId, e.IsDefault })
                .HasFilter("[IsDefault] = 1");
        });
    }
}
