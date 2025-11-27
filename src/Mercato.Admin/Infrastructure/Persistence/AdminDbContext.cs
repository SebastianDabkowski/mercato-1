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
    }
}
