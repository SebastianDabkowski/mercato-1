using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Payments module.
/// </summary>
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    // TODO: Define DbSet<Transaction> when Transaction entity is implemented
    // public DbSet<Transaction> Transactions { get; set; }

    // TODO: Define DbSet<Refund> when Refund entity is implemented
    // public DbSet<Refund> Refunds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO: Configure entity mappings when entities are defined
    }
}
