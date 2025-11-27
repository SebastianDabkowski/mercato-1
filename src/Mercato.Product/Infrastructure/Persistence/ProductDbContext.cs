using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Persistence;

/// <summary>
/// DbContext for the Product module.
/// </summary>
public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    // TODO: Define DbSet<Product> when Product entity is implemented
    // public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO: Configure entity mappings when entities are defined
    }
}
