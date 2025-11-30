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

    /// <summary>
    /// Gets or sets the message threads DbSet.
    /// </summary>
    public DbSet<MessageThread> MessageThreads { get; set; }

    /// <summary>
    /// Gets or sets the messages DbSet.
    /// </summary>
    public DbSet<Message> Messages { get; set; }

    /// <summary>
    /// Gets or sets the push subscriptions DbSet.
    /// </summary>
    public DbSet<PushSubscription> PushSubscriptions { get; set; }

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

        modelBuilder.Entity<MessageThread>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BuyerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.SellerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);

            // Relationship with Messages
            entity.HasMany(e => e.Messages)
                .WithOne(e => e.Thread)
                .HasForeignKey(e => e.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for product Q&A queries
            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("IX_MessageThreads_ProductId");

            // Index for order-related messaging
            entity.HasIndex(e => e.OrderId)
                .HasDatabaseName("IX_MessageThreads_OrderId");

            // Index for buyer threads
            entity.HasIndex(e => e.BuyerId)
                .HasDatabaseName("IX_MessageThreads_BuyerId");

            // Index for seller threads
            entity.HasIndex(e => e.SellerId)
                .HasDatabaseName("IX_MessageThreads_SellerId");

            // Composite index for buyer threads ordering
            entity.HasIndex(e => new { e.BuyerId, e.LastMessageAt })
                .HasDatabaseName("IX_MessageThreads_BuyerId_LastMessageAt");

            // Composite index for seller threads ordering
            entity.HasIndex(e => new { e.SellerId, e.LastMessageAt })
                .HasDatabaseName("IX_MessageThreads_SellerId_LastMessageAt");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SenderId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(4000);

            // Index for thread messages ordering
            entity.HasIndex(e => new { e.ThreadId, e.CreatedAt })
                .HasDatabaseName("IX_Messages_ThreadId_CreatedAt");
        });

        modelBuilder.Entity<PushSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.P256DH).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Auth).IsRequired().HasMaxLength(500);

            // Index for efficient user subscription queries
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_PushSubscriptions_UserId");

            // Index for endpoint lookups (to avoid duplicates)
            entity.HasIndex(e => e.Endpoint)
                .HasDatabaseName("IX_PushSubscriptions_Endpoint");
        });
    }
}
