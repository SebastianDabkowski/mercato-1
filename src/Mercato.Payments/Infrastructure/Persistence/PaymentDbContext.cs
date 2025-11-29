using Mercato.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Payments module.
/// </summary>
public class PaymentDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the escrow entries.
    /// </summary>
    public DbSet<EscrowEntry> EscrowEntries { get; set; } = null!;

    // TODO: Define DbSet<Transaction> when Transaction entity is implemented
    // public DbSet<Transaction> Transactions { get; set; }

    // TODO: Define DbSet<Refund> when Refund entity is implemented
    // public DbSet<Refund> Refunds { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure EscrowEntry entity
        modelBuilder.Entity<EscrowEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();
            
            entity.Property(e => e.AuditNote)
                .HasMaxLength(500);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.PaymentTransactionId);
            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => new { e.SellerId, e.Status });
        });

        // TODO: Configure other entity mappings when entities are defined
    }
}
