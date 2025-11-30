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

    /// <summary>
    /// Gets or sets the consent types DbSet.
    /// </summary>
    public DbSet<ConsentType> ConsentTypes { get; set; }

    /// <summary>
    /// Gets or sets the consent versions DbSet.
    /// </summary>
    public DbSet<ConsentVersion> ConsentVersions { get; set; }

    /// <summary>
    /// Gets or sets the user consents DbSet.
    /// </summary>
    public DbSet<UserConsent> UserConsents { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDeliveryAddress(modelBuilder);
        ConfigureConsentType(modelBuilder);
        ConfigureConsentVersion(modelBuilder);
        ConfigureUserConsent(modelBuilder);
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

    private static void ConfigureConsentType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsentType>(entity =>
        {
            entity.ToTable("ConsentTypes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.IsMandatory)
                .IsRequired();

            entity.Property(e => e.DisplayOrder)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.Code)
                .IsUnique();

            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureConsentVersion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsentVersion>(entity =>
        {
            entity.ToTable("ConsentVersions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ConsentTypeId)
                .IsRequired();

            entity.Property(e => e.VersionNumber)
                .IsRequired();

            entity.Property(e => e.ConsentText)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(e => e.EffectiveFrom)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(e => e.ConsentType)
                .WithMany(ct => ct.Versions)
                .HasForeignKey(e => e.ConsentTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ConsentTypeId, e.VersionNumber })
                .IsUnique();

            entity.HasIndex(e => new { e.ConsentTypeId, e.EffectiveTo });
        });
    }

    private static void ConfigureUserConsent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserConsent>(entity =>
        {
            entity.ToTable("UserConsents");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.ConsentVersionId)
                .IsRequired();

            entity.Property(e => e.IsGranted)
                .IsRequired();

            entity.Property(e => e.ConsentedAt)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.HasOne(e => e.ConsentVersion)
                .WithMany(cv => cv.UserConsents)
                .HasForeignKey(e => e.ConsentVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);

            entity.HasIndex(e => new { e.UserId, e.ConsentVersionId });

            entity.HasIndex(e => e.ConsentedAt);
        });
    }
}
