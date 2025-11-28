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

    /// <summary>
    /// Gets or sets the categories DbSet.
    /// </summary>
    public DbSet<Category> Categories { get; set; }

    /// <summary>
    /// Gets or sets the product import jobs DbSet.
    /// </summary>
    public DbSet<ProductImportJob> ProductImportJobs { get; set; }

    /// <summary>
    /// Gets or sets the product import row errors DbSet.
    /// </summary>
    public DbSet<ProductImportRowError> ProductImportRowErrors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProduct(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureProductImportJob(modelBuilder);
        ConfigureProductImportRowError(modelBuilder);
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

            entity.Property(e => e.Sku)
                .HasMaxLength(100);

            // Index for querying products by store
            entity.HasIndex(e => e.StoreId);

            // Index for querying products by status
            entity.HasIndex(e => e.Status);

            // Index for filtering archived products by store
            entity.HasIndex(e => new { e.StoreId, e.Status });

            // Index for efficient category lookups (used by category management)
            entity.HasIndex(e => e.Category);

            // Unique index for SKU within store (for import matching)
            entity.HasIndex(e => new { e.StoreId, e.Sku })
                .IsUnique()
                .HasFilter("[Sku] IS NOT NULL");
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(ProductValidationConstants.CategoryNameMaxLength);

            entity.Property(e => e.ParentId);

            entity.Property(e => e.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdatedAt)
                .IsRequired();

            // Index for querying categories by parent
            entity.HasIndex(e => e.ParentId);

            // Index for active categories
            entity.HasIndex(e => e.IsActive);

            // Unique index for name within parent to enforce uniqueness
            entity.HasIndex(e => new { e.ParentId, e.Name })
                .IsUnique();
        });
    }

    private static void ConfigureProductImportJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductImportJob>(entity =>
        {
            entity.ToTable("ProductImportJobs");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.StoreId)
                .IsRequired();

            entity.Property(e => e.SellerId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.TotalRows)
                .IsRequired();

            entity.Property(e => e.NewProductsCount)
                .IsRequired();

            entity.Property(e => e.UpdatedProductsCount)
                .IsRequired();

            entity.Property(e => e.ErrorCount)
                .IsRequired();

            entity.Property(e => e.SuccessCount)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            // Index for querying jobs by store
            entity.HasIndex(e => e.StoreId);

            // Index for querying jobs by status
            entity.HasIndex(e => e.Status);

            // Relationship to row errors
            entity.HasMany(e => e.RowErrors)
                .WithOne(e => e.ImportJob)
                .HasForeignKey(e => e.ImportJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductImportRowError(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductImportRowError>(entity =>
        {
            entity.ToTable("ProductImportRowErrors");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ImportJobId)
                .IsRequired();

            entity.Property(e => e.RowNumber)
                .IsRequired();

            entity.Property(e => e.ColumnName)
                .HasMaxLength(100);

            entity.Property(e => e.ErrorMessage)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Sku)
                .HasMaxLength(100);

            // Index for querying errors by job
            entity.HasIndex(e => e.ImportJobId);
        });
    }
}
