namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents a specific product variant that is a combination of variant attribute values.
/// Each variant can have its own SKU, stock, price, and images.
/// For example: A "Blue T-Shirt Size Medium" would be a variant of a T-Shirt product.
/// </summary>
public class ProductVariant
{
    /// <summary>
    /// Gets or sets the unique identifier for the variant.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this variant belongs to.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the Stock Keeping Unit (SKU) for this variant.
    /// Must be unique within the store.
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the price for this variant.
    /// If null, the product's base price is used.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Gets or sets the available stock quantity for this variant.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the variant images as a JSON array of image URLs.
    /// If null, the product's base images are used.
    /// </summary>
    public string? Images { get; set; }

    /// <summary>
    /// Gets or sets the combination of attribute values as a JSON object.
    /// Format: {"Size": "Medium", "Color": "Blue"}
    /// </summary>
    public string AttributeCombination { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this variant is active.
    /// Inactive variants are not available for purchase.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the variant was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the variant was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent product.
    /// </summary>
    public Domain.Entities.Product? Product { get; set; }
}
