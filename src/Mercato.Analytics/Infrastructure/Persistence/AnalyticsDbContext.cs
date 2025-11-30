using Mercato.Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Analytics.Infrastructure.Persistence;

/// <summary>
/// Database context for Analytics module.
/// </summary>
public class AnalyticsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the analytics events.
    /// </summary>
    public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AnalyticsEvent>(entity =>
        {
            entity.ToTable("AnalyticsEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.SearchQuery).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasMaxLength(4000);

            // Indexes for efficient querying
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.EventType, e.Timestamp })
                .HasDatabaseName("IX_AnalyticsEvents_EventType_Timestamp");
        });
    }
}
