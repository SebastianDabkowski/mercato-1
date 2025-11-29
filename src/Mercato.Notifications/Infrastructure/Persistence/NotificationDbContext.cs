using Mercato.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Notifications.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Notifications module.
/// </summary>
public class NotificationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the notifications DbSet.
    /// </summary>
    public DbSet<Notification> Notifications { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.RelatedUrl).HasMaxLength(500);

            // Index for efficient user notification queries
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_Notifications_UserId");

            // Composite index for filtering by user and read status
            entity.HasIndex(e => new { e.UserId, e.IsRead })
                .HasDatabaseName("IX_Notifications_UserId_IsRead");

            // Composite index for pagination and ordering
            entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                .HasDatabaseName("IX_Notifications_UserId_CreatedAt");
        });
    }
}
