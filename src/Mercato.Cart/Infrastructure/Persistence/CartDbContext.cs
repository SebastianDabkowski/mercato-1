using Microsoft.EntityFrameworkCore;

namespace Mercato.Cart.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Cart module.
/// </summary>
public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options)
        : base(options)
    {
    }

    // TODO: Define DbSet<Cart> when Cart entity is implemented
    // public DbSet<Cart> Carts { get; set; }

    // TODO: Define DbSet<CartItem> when CartItem entity is implemented
    // public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO: Configure entity mappings when entities are defined
    }
}
