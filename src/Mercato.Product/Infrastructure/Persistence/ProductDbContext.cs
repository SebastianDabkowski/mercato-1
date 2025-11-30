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

    /// <summary>
    /// Gets or sets the product images DbSet.
    /// </summary>
    public DbSet<ProductImage> ProductImages { get; set; }

    /// <summary>
    /// Gets or sets the product variant attributes DbSet.
    /// </summary>
    public DbSet<ProductVariantAttribute> ProductVariantAttributes { get; set; }

    /// <summary>
    /// Gets or sets the product variant attribute values DbSet.
    /// </summary>
    public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues { get; set; }

    /// <summary>
    /// Gets or sets the product variants DbSet.
    /// </summary>
    public DbSet<ProductVariant> ProductVariants { get; set; }

    /// <summary>
    /// Gets or sets the product moderation decisions DbSet.
    /// </summary>
    public DbSet<ProductModerationDecision> ProductModerationDecisions { get; set; }

    /// <summary>
    /// Gets or sets the photo moderation decisions DbSet.
    /// </summary>
    public DbSet<PhotoModerationDecision> PhotoModerationDecisions { get; set; }

    /// <summary>
    /// Gets or sets the category attributes DbSet.
    /// </summary>
    public DbSet<CategoryAttribute> CategoryAttributes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProduct(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureProductImportJob(modelBuilder);
        ConfigureProductImportRowError(modelBuilder);
        ConfigureProductImage(modelBuilder);
        ConfigureProductVariantAttribute(modelBuilder);
        ConfigureProductVariantAttributeValue(modelBuilder);
        ConfigureProductVariant(modelBuilder);
        ConfigureProductModerationDecision(modelBuilder);
        ConfigurePhotoModerationDecision(modelBuilder);
        ConfigureCategoryAttribute(modelBuilder);
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

            entity.Property(e => e.HasVariants)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.ModerationStatus)
                .IsRequired()
                .HasDefaultValue(ProductModerationStatus.NotSubmitted);

            entity.Property(e => e.ModerationReason)
                .HasMaxLength(2000);

            entity.Property(e => e.ModeratedAt);

            entity.Property(e => e.ModeratedBy)
                .HasMaxLength(450);

            // Index for querying products by store
            entity.HasIndex(e => e.StoreId);

            // Index for querying products by status
            entity.HasIndex(e => e.Status);

            // Index for filtering archived products by store
            entity.HasIndex(e => new { e.StoreId, e.Status });

            // Index for efficient category lookups (used by category management)
            entity.HasIndex(e => e.Category);

            // Index for moderation queue queries
            entity.HasIndex(e => e.ModerationStatus);

            // Index for moderation queue with category filter
            entity.HasIndex(e => new { e.ModerationStatus, e.Category });

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

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(ProductValidationConstants.CategorySlugMaxLength);

            entity.Property(e => e.Description)
                .HasMaxLength(ProductValidationConstants.CategoryDescriptionMaxLength);

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

            // Unique index for slug to ensure SEO-friendly URLs are unique
            entity.HasIndex(e => e.Slug)
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

            entity.Property(e => e.ImportDataJson);

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

    private static void ConfigureProductImage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(ProductImageValidationConstants.FileNameMaxLength);

            entity.Property(e => e.StoragePath)
                .IsRequired()
                .HasMaxLength(ProductImageValidationConstants.StoragePathMaxLength);

            entity.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(ProductImageValidationConstants.ContentTypeMaxLength);

            entity.Property(e => e.FileSize)
                .IsRequired();

            entity.Property(e => e.IsMain)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ThumbnailPath)
                .HasMaxLength(ProductImageValidationConstants.StoragePathMaxLength);

            entity.Property(e => e.OptimizedPath)
                .HasMaxLength(ProductImageValidationConstants.StoragePathMaxLength);

            // Moderation fields
            entity.Property(e => e.ModerationStatus)
                .IsRequired()
                .HasDefaultValue(PhotoModerationStatus.PendingReview);

            entity.Property(e => e.ModerationReason)
                .HasMaxLength(2000);

            entity.Property(e => e.ModeratedAt);

            entity.Property(e => e.ModeratedBy)
                .HasMaxLength(450);

            entity.Property(e => e.IsFlagged)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.FlagReason)
                .HasMaxLength(500);

            entity.Property(e => e.FlaggedAt);

            // Index for querying images by product
            entity.HasIndex(e => e.ProductId);

            // Index for finding main image
            entity.HasIndex(e => new { e.ProductId, e.IsMain });

            // Index for moderation queue queries
            entity.HasIndex(e => e.ModerationStatus);

            // Index for flagged photos in moderation queue
            entity.HasIndex(e => new { e.ModerationStatus, e.IsFlagged });

            // Relationship to product
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductVariantAttribute(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductVariantAttribute>(entity =>
        {
            entity.ToTable("ProductVariantAttributes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(ProductVariantValidationConstants.AttributeNameMaxLength);

            entity.Property(e => e.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Index for querying attributes by product
            entity.HasIndex(e => e.ProductId);

            // Unique index for attribute name within product
            entity.HasIndex(e => new { e.ProductId, e.Name })
                .IsUnique();

            // Relationship to product
            entity.HasOne(e => e.Product)
                .WithMany(p => p.VariantAttributes)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductVariantAttributeValue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductVariantAttributeValue>(entity =>
        {
            entity.ToTable("ProductVariantAttributeValues");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.VariantAttributeId)
                .IsRequired();

            entity.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(ProductVariantValidationConstants.AttributeValueMaxLength);

            entity.Property(e => e.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Index for querying values by attribute
            entity.HasIndex(e => e.VariantAttributeId);

            // Unique index for value within attribute
            entity.HasIndex(e => new { e.VariantAttributeId, e.Value })
                .IsUnique();

            // Relationship to variant attribute
            entity.HasOne(e => e.VariantAttribute)
                .WithMany(a => a.Values)
                .HasForeignKey(e => e.VariantAttributeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductVariant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariants");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.Property(e => e.Sku)
                .HasMaxLength(100);

            entity.Property(e => e.Price)
                .HasPrecision(18, 2);

            entity.Property(e => e.Stock)
                .IsRequired();

            entity.Property(e => e.Images)
                .HasMaxLength(ProductValidationConstants.ImagesMaxLength);

            entity.Property(e => e.AttributeCombination)
                .IsRequired()
                .HasMaxLength(ProductVariantValidationConstants.AttributeCombinationMaxLength);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdatedAt)
                .IsRequired();

            // Index for querying variants by product
            entity.HasIndex(e => e.ProductId);

            // Index for querying active variants
            entity.HasIndex(e => new { e.ProductId, e.IsActive });

            // Relationship to product
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductModerationDecision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductModerationDecision>(entity =>
        {
            entity.ToTable("ProductModerationDecisions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.Property(e => e.AdminUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.Decision)
                .IsRequired();

            entity.Property(e => e.Reason)
                .HasMaxLength(2000);

            entity.Property(e => e.PreviousStatus)
                .IsRequired();

            entity.Property(e => e.PreviousProductStatus)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            // Index for querying decisions by product
            entity.HasIndex(e => e.ProductId);

            // Index for querying by admin user
            entity.HasIndex(e => e.AdminUserId);

            // Index for ordering by creation date
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private static void ConfigurePhotoModerationDecision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PhotoModerationDecision>(entity =>
        {
            entity.ToTable("PhotoModerationDecisions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductImageId)
                .IsRequired();

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.Property(e => e.StoreId)
                .IsRequired();

            entity.Property(e => e.AdminUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.Decision)
                .IsRequired();

            entity.Property(e => e.Reason)
                .HasMaxLength(2000);

            entity.Property(e => e.PreviousStatus)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            // Index for querying decisions by photo
            entity.HasIndex(e => e.ProductImageId);

            // Index for querying decisions by product
            entity.HasIndex(e => e.ProductId);

            // Index for querying by admin user
            entity.HasIndex(e => e.AdminUserId);

            // Index for ordering by creation date
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private static void ConfigureCategoryAttribute(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoryAttribute>(entity =>
        {
            entity.ToTable("CategoryAttributes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.CategoryId)
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Type)
                .IsRequired();

            entity.Property(e => e.IsRequired)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.IsDeprecated)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.ListOptions)
                .HasMaxLength(4000);

            entity.Property(e => e.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.LastUpdatedAt)
                .IsRequired();

            // Index for querying attributes by category
            entity.HasIndex(e => e.CategoryId)
                .HasDatabaseName("IX_CategoryAttributes_CategoryId");

            // Index for active (non-deprecated) attributes
            entity.HasIndex(e => new { e.CategoryId, e.IsDeprecated })
                .HasDatabaseName("IX_CategoryAttributes_CategoryId_IsDeprecated");

            // Unique index for attribute name within category
            entity.HasIndex(e => new { e.CategoryId, e.Name })
                .IsUnique()
                .HasDatabaseName("IX_CategoryAttributes_CategoryId_Name");

            // Relationship to category
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
