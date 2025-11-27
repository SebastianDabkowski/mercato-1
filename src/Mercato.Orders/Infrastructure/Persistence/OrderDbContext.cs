using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Orders module.
/// </summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    // TODO: Define DbSet<Order> when Order entity is implemented
    // public DbSet<Order> Orders { get; set; }

    // TODO: Define DbSet<OrderItem> when OrderItem entity is implemented
    // public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO: Configure entity mappings when entities are defined
    }
}
