using Mercato.Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Persistence;

// TODO: FIX it: AdminDbContext has entities defined (RoleChangeAuditLog, AuthenticationEvent) but no migrations folder.
// Create migrations using: dotnet ef migrations add InitialAdminSchema -p Mercato.Admin -s Mercato.Web -c AdminDbContext
/// <summary>
/// Database context for Admin module.
/// </summary>
public class AdminDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the role change audit logs.
    /// </summary>
    public DbSet<RoleChangeAuditLog> RoleChangeAuditLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the authentication events.
    /// </summary>
    public DbSet<AuthenticationEvent> AuthenticationEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SLA configurations.
    /// </summary>
    public DbSet<SlaConfiguration> SlaConfigurations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SLA tracking records.
    /// </summary>
    public DbSet<SlaTrackingRecord> SlaTrackingRecords { get; set; } = null!;

    /// <summary>
    /// Gets or sets the admin audit logs.
    /// </summary>
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user block information records.
    /// </summary>
    public DbSet<UserBlockInfo> UserBlockInfos { get; set; } = null!;

    /// <summary>
    /// Gets or sets the VAT rules.
    /// </summary>
    public DbSet<VatRule> VatRules { get; set; } = null!;

    /// <summary>
    /// Gets or sets the VAT rule history records.
    /// </summary>
    public DbSet<VatRuleHistory> VatRuleHistories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the currencies.
    /// </summary>
    public DbSet<Currency> Currencies { get; set; } = null!;

    /// <summary>
    /// Gets or sets the currency history records.
    /// </summary>
    public DbSet<CurrencyHistory> CurrencyHistories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the integrations.
    /// </summary>
    public DbSet<Integration> Integrations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the legal documents.
    /// </summary>
    public DbSet<LegalDocument> LegalDocuments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the legal document versions.
    /// </summary>
    public DbSet<LegalDocumentVersion> LegalDocumentVersions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the legal consents.
    /// </summary>
    public DbSet<LegalConsent> LegalConsents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the feature flags.
    /// </summary>
    public DbSet<FeatureFlag> FeatureFlags { get; set; } = null!;

    /// <summary>
    /// Gets or sets the feature flag history records.
    /// </summary>
    public DbSet<FeatureFlagHistory> FeatureFlagHistories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the data processing activities for GDPR compliance.
    /// </summary>
    public DbSet<DataProcessingActivity> DataProcessingActivities { get; set; } = null!;

    /// <summary>
    /// Gets or sets the data processing activity history records.
    /// </summary>
    public DbSet<DataProcessingActivityHistory> DataProcessingActivityHistories { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RoleChangeAuditLog>(entity =>
        {
            entity.ToTable("RoleChangeAuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.OldRole).IsRequired().HasMaxLength(256);
            entity.Property(e => e.NewRole).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Details).HasMaxLength(1000);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PerformedAt);
        });

        modelBuilder.Entity<AuthenticationEvent>(entity =>
        {
            entity.ToTable("AuthenticationEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.UserRole).HasMaxLength(50);
            entity.Property(e => e.IpAddressHash).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IpAddressHash);
            entity.HasIndex(e => e.IsSuccessful);
        });

        modelBuilder.Entity<SlaConfiguration>(entity =>
        {
            entity.ToTable("SlaConfigurations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CaseType).HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => new { e.CaseType, e.Category })
                .HasDatabaseName("IX_SlaConfigurations_CaseType_Category");
        });

        modelBuilder.Entity<SlaTrackingRecord>(entity =>
        {
            entity.ToTable("SlaTrackingRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CaseNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CaseType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StoreName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.CaseId).IsUnique();
            entity.HasIndex(e => e.CaseNumber);
            entity.HasIndex(e => e.StoreId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CaseCreatedAt);
            entity.HasIndex(e => e.IsFirstResponseBreached);
            entity.HasIndex(e => e.IsResolutionBreached);
            entity.HasIndex(e => new { e.StoreId, e.CaseCreatedAt })
                .HasDatabaseName("IX_SlaTrackingRecords_StoreId_CaseCreatedAt");
            entity.HasIndex(e => new { e.Status, e.IsFirstResponseBreached, e.IsResolutionBreached })
                .HasDatabaseName("IX_SlaTrackingRecords_Status_Breaches");
            // Ignore computed properties
            entity.Ignore(e => e.ResponseTimeHours);
            entity.Ignore(e => e.ResolutionTimeHours);
        });

        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("AdminAuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdminUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.FailureReason).HasMaxLength(1000);
            entity.HasIndex(e => e.AdminUserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.IsSuccess);
            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("IX_AdminAuditLogs_EntityType_EntityId");
            entity.HasIndex(e => new { e.Timestamp, e.IsSuccess })
                .HasDatabaseName("IX_AdminAuditLogs_Timestamp_IsSuccess");
        });

        modelBuilder.Entity<UserBlockInfo>(entity =>
        {
            entity.ToTable("UserBlockInfos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.BlockedByAdminId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.BlockedByAdminEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ReasonDetails).HasMaxLength(2000);
            entity.Property(e => e.UnblockedByAdminId).HasMaxLength(450);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.UserId, e.IsActive })
                .HasDatabaseName("IX_UserBlockInfos_UserId_IsActive");
        });

        modelBuilder.Entity<VatRule>(entity =>
        {
            entity.ToTable("VatRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(2);
            entity.Property(e => e.TaxRate).HasPrecision(5, 2);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.CountryCode);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.EffectiveFrom);
            entity.HasIndex(e => new { e.CountryCode, e.CategoryId, e.IsActive })
                .HasDatabaseName("IX_VatRules_CountryCode_CategoryId_IsActive");
            entity.HasIndex(e => new { e.CountryCode, e.IsActive, e.EffectiveFrom, e.EffectiveTo })
                .HasDatabaseName("IX_VatRules_CountryCode_IsActive_EffectiveDates");
        });

        modelBuilder.Entity<VatRuleHistory>(entity =>
        {
            entity.ToTable("VatRuleHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PreviousValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ChangedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ChangedByUserEmail).HasMaxLength(256);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.HasIndex(e => e.VatRuleId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => new { e.VatRuleId, e.ChangedAt })
                .HasDatabaseName("IX_VatRuleHistories_VatRuleId_ChangedAt");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.ToTable("Currencies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(5);
            entity.Property(e => e.ExchangeRateToBase).HasPrecision(18, 6);
            entity.Property(e => e.ExchangeRateSource).HasMaxLength(100);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsBaseCurrency);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => new { e.IsEnabled, e.IsBaseCurrency })
                .HasDatabaseName("IX_Currencies_IsEnabled_IsBaseCurrency");
        });

        modelBuilder.Entity<CurrencyHistory>(entity =>
        {
            entity.ToTable("CurrencyHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PreviousValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ChangedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ChangedByUserEmail).HasMaxLength(256);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.HasIndex(e => e.CurrencyId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => new { e.CurrencyId, e.ChangedAt })
                .HasDatabaseName("IX_CurrencyHistories_CurrencyId_ChangedAt");
        });

        modelBuilder.Entity<Integration>(entity =>
        {
            entity.ToTable("Integrations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ApiEndpoint).HasMaxLength(500);
            entity.Property(e => e.ApiKeyMasked).HasMaxLength(200);
            entity.Property(e => e.MerchantId).HasMaxLength(100);
            entity.Property(e => e.CallbackUrl).HasMaxLength(500);
            entity.Property(e => e.LastHealthCheckMessage).HasMaxLength(1000);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IntegrationType);
            entity.HasIndex(e => e.Environment);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.IntegrationType, e.Environment })
                .HasDatabaseName("IX_Integrations_IntegrationType_Environment");
            entity.HasIndex(e => new { e.IsEnabled, e.Status })
                .HasDatabaseName("IX_Integrations_IsEnabled_Status");
        });

        modelBuilder.Entity<LegalDocument>(entity =>
        {
            entity.ToTable("LegalDocuments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.DocumentType).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<LegalDocumentVersion>(entity =>
        {
            entity.ToTable("LegalDocumentVersions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VersionNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ChangeSummary).HasMaxLength(2000);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.LegalDocumentId);
            entity.HasIndex(e => e.IsPublished);
            entity.HasIndex(e => e.EffectiveDate);
            entity.HasIndex(e => new { e.LegalDocumentId, e.VersionNumber })
                .HasDatabaseName("IX_LegalDocumentVersions_DocumentId_VersionNumber").IsUnique();
            entity.HasIndex(e => new { e.LegalDocumentId, e.IsPublished, e.EffectiveDate })
                .HasDatabaseName("IX_LegalDocumentVersions_DocumentId_Published_EffectiveDate");
        });

        modelBuilder.Entity<LegalConsent>(entity =>
        {
            entity.ToTable("LegalConsents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.VersionNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IpAddressHash).HasMaxLength(64);
            entity.Property(e => e.ConsentContext).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LegalDocumentVersionId);
            entity.HasIndex(e => e.DocumentType);
            entity.HasIndex(e => e.ConsentedAt);
            entity.HasIndex(e => new { e.UserId, e.DocumentType })
                .HasDatabaseName("IX_LegalConsents_UserId_DocumentType");
            entity.HasIndex(e => new { e.UserId, e.LegalDocumentVersionId })
                .HasDatabaseName("IX_LegalConsents_UserId_VersionId");
        });

        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.ToTable("FeatureFlags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TargetValue).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.Key);
            entity.HasIndex(e => e.Environment);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => new { e.Key, e.Environment })
                .HasDatabaseName("IX_FeatureFlags_Key_Environment").IsUnique();
            entity.HasIndex(e => new { e.Environment, e.IsEnabled })
                .HasDatabaseName("IX_FeatureFlags_Environment_IsEnabled");
        });

        modelBuilder.Entity<FeatureFlagHistory>(entity =>
        {
            entity.ToTable("FeatureFlagHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PreviousValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ChangedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ChangedByUserEmail).HasMaxLength(256);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.HasIndex(e => e.FeatureFlagId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => new { e.FeatureFlagId, e.ChangedAt })
                .HasDatabaseName("IX_FeatureFlagHistories_FeatureFlagId_ChangedAt");
        });

        modelBuilder.Entity<DataProcessingActivity>(entity =>
        {
            entity.ToTable("DataProcessingActivities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Purpose).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.LegalBasis).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DataCategories).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.DataSubjectCategories).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Recipients).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ThirdCountryTransfers).HasMaxLength(2000);
            entity.Property(e => e.RetentionPeriod).IsRequired().HasMaxLength(500);
            entity.Property(e => e.TechnicalMeasures).HasMaxLength(2000);
            entity.Property(e => e.OrganizationalMeasures).HasMaxLength(2000);
            entity.Property(e => e.ProcessorName).HasMaxLength(200);
            entity.Property(e => e.ProcessorContact).HasMaxLength(500);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.IsActive, e.CreatedAt })
                .HasDatabaseName("IX_DataProcessingActivities_IsActive_CreatedAt");
        });

        modelBuilder.Entity<DataProcessingActivityHistory>(entity =>
        {
            entity.ToTable("DataProcessingActivityHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PreviousValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.ChangedByUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ChangedByUserEmail).HasMaxLength(256);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.HasIndex(e => e.DataProcessingActivityId);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => new { e.DataProcessingActivityId, e.ChangedAt })
                .HasDatabaseName("IX_DataProcessingActivityHistories_ActivityId_ChangedAt");
        });
    }
}
