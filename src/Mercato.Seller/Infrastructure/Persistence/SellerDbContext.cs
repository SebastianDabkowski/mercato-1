using Mercato.Seller.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Persistence;

// TODO: FIX it: SellerDbContext has entities defined (KycSubmission, KycAuditLog) but no migrations folder.
// Create migrations using: dotnet ef migrations add InitialSellerSchema -p Mercato.Seller -s Mercato.Web -c SellerDbContext
/// <summary>
/// DbContext for the Seller module.
/// </summary>
public class SellerDbContext : DbContext
{
    public SellerDbContext(DbContextOptions<SellerDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the KYC submissions.
    /// </summary>
    public DbSet<KycSubmission> KycSubmissions { get; set; }

    /// <summary>
    /// Gets or sets the KYC audit logs.
    /// </summary>
    public DbSet<KycAuditLog> KycAuditLogs { get; set; }

    /// <summary>
    /// Gets or sets the seller onboarding records.
    /// </summary>
    public DbSet<SellerOnboarding> SellerOnboardings { get; set; }

    /// <summary>
    /// Gets or sets the stores.
    /// </summary>
    public DbSet<Store> Stores { get; set; }

    /// <summary>
    /// Gets or sets the payout settings.
    /// </summary>
    public DbSet<PayoutSettings> PayoutSettings { get; set; }

    /// <summary>
    /// Gets or sets the store users (internal team members).
    /// </summary>
    public DbSet<StoreUser> StoreUsers { get; set; }

    /// <summary>
    /// Gets or sets the shipping rules.
    /// </summary>
    public DbSet<ShippingRule> ShippingRules { get; set; }

    /// <summary>
    /// Gets or sets the shipping methods.
    /// </summary>
    public DbSet<ShippingMethod> ShippingMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure KycSubmission entity
        modelBuilder.Entity<KycSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SellerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.DocumentFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DocumentContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DocumentData).IsRequired();
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.ReviewedBy).HasMaxLength(450);

            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.Status);
        });

        // Configure KycAuditLog entity
        modelBuilder.Entity<KycAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Details).HasMaxLength(2000);

            entity.HasIndex(e => e.KycSubmissionId);
            entity.HasOne<KycSubmission>()
                .WithMany()
                .HasForeignKey(e => e.KycSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SellerOnboarding entity
        modelBuilder.Entity<SellerOnboarding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SellerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.StoreName).HasMaxLength(200);
            entity.Property(e => e.StoreDescription).HasMaxLength(2000);
            entity.Property(e => e.StoreLogoUrl).HasMaxLength(500);
            entity.Property(e => e.BusinessName).HasMaxLength(200);
            entity.Property(e => e.BusinessAddress).HasMaxLength(500);
            entity.Property(e => e.TaxId).HasMaxLength(50);
            entity.Property(e => e.BusinessRegistrationNumber).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.PersonalIdNumber).HasMaxLength(50);
            entity.Property(e => e.ContactPersonName).HasMaxLength(200);
            entity.Property(e => e.ContactPersonEmail).HasMaxLength(254);
            entity.Property(e => e.ContactPersonPhone).HasMaxLength(20);
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.BankAccountNumber).HasMaxLength(50);
            entity.Property(e => e.BankRoutingNumber).HasMaxLength(20);
            entity.Property(e => e.AccountHolderName).HasMaxLength(200);

            entity.HasIndex(e => e.SellerId).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        // Configure Store entity
        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SellerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.ContactEmail).HasMaxLength(254);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(500);

            entity.HasIndex(e => e.SellerId).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Configure PayoutSettings entity
        modelBuilder.Entity<PayoutSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SellerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.BankAccountNumber).HasMaxLength(50);
            entity.Property(e => e.BankRoutingNumber).HasMaxLength(20);
            entity.Property(e => e.AccountHolderName).HasMaxLength(200);
            entity.Property(e => e.SwiftCode).HasMaxLength(11);
            entity.Property(e => e.Iban).HasMaxLength(34);
            entity.Property(e => e.PaymentAccountEmail).HasMaxLength(254);
            entity.Property(e => e.PaymentAccountId).HasMaxLength(100);

            entity.HasIndex(e => e.SellerId).IsUnique();
        });

        // Configure StoreUser entity
        modelBuilder.Entity<StoreUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StoreId).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(254);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.InvitationToken).HasMaxLength(100);
            entity.Property(e => e.InvitedBy).IsRequired().HasMaxLength(450);
            entity.Property(e => e.DeactivatedBy).HasMaxLength(450);
            entity.Property(e => e.RoleChangedBy).HasMaxLength(450);

            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.InvitationToken).IsUnique().HasFilter("[InvitationToken] IS NOT NULL");
            entity.HasIndex(e => new { e.StoreId, e.Email }).IsUnique();

            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ShippingRule entity
        modelBuilder.Entity<ShippingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StoreId).IsRequired();
            entity.Property(e => e.FlatRate).HasPrecision(18, 2);
            entity.Property(e => e.FreeShippingThreshold).HasPrecision(18, 2);
            entity.Property(e => e.PerItemRate).HasPrecision(18, 2);

            entity.HasIndex(e => e.StoreId).IsUnique();
        });

        // Configure ShippingMethod entity
        modelBuilder.Entity<ShippingMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StoreId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.AvailableCountries).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.BaseCost).HasPrecision(18, 2);

            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => new { e.StoreId, e.IsActive });
        });
    }
}
