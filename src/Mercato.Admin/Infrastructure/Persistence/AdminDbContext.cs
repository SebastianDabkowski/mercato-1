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
    }
}
