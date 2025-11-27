using Mercato.Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Persistence;

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
    }
}
