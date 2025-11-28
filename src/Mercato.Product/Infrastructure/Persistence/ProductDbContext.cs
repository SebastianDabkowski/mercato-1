using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
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

    /// <summary>
    /// Gets or sets the products DbSet.
    /// </summary>
    public DbSet<Domain.Entities.Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProduct(modelBuilder);
    }

    private static void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Entities.Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.StoreId)
                .IsRequired();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(e => e.Stock)
                .IsRequired();

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(ProductStatus.Draft);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdatedBy)
                .HasMaxLength(450);

            entity.Property(e => e.ArchivedAt);

            entity.Property(e => e.ArchivedBy)
                .HasMaxLength(450);

            // Shipping parameters
            entity.Property(e => e.Weight)
                .HasPrecision(10, 3);

            entity.Property(e => e.Length)
                .HasPrecision(10, 2);

            entity.Property(e => e.Width)
                .HasPrecision(10, 2);

            entity.Property(e => e.Height)
                .HasPrecision(10, 2);

            entity.Property(e => e.ShippingMethods)
                .HasMaxLength(ProductValidationConstants.ShippingMethodsMaxLength);

            entity.Property(e => e.Images)
                .HasMaxLength(ProductValidationConstants.ImagesMaxLength);

            // Index for querying products by store
            entity.HasIndex(e => e.StoreId);

            // Index for querying products by status
            entity.HasIndex(e => e.Status);

            // Index for filtering archived products by store
            entity.HasIndex(e => new { e.StoreId, e.Status });
        });
    }
}
