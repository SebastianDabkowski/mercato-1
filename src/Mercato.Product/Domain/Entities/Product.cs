namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents a product in the marketplace catalog.
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the unique identifier for the product.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the available stock quantity.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product status for visibility control.
    /// </summary>
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    /// <summary>
    /// Gets or sets the date and time when the product was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the product was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last updated the product.
    /// </summary>
    public string? LastUpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the product was archived.
    /// </summary>
    public DateTimeOffset? ArchivedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who archived the product.
    /// </summary>
    public string? ArchivedBy { get; set; }

    /// <summary>
    /// Gets or sets the product weight in kilograms.
    /// Used for shipping cost calculation.
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Gets or sets the product length in centimeters.
    /// Used for shipping cost calculation.
    /// </summary>
    public decimal? Length { get; set; }

    /// <summary>
    /// Gets or sets the product width in centimeters.
    /// Used for shipping cost calculation.
    /// </summary>
    public decimal? Width { get; set; }

    /// <summary>
    /// Gets or sets the product height in centimeters.
    /// Used for shipping cost calculation.
    /// </summary>
    public decimal? Height { get; set; }

    /// <summary>
    /// Gets or sets the available shipping methods for this product.
    /// Stored as a comma-separated string of shipping method codes.
    /// </summary>
    public string? ShippingMethods { get; set; }

    /// <summary>
    /// Gets or sets the product images as a JSON array of image URLs.
    /// </summary>
    public string? Images { get; set; }
}
