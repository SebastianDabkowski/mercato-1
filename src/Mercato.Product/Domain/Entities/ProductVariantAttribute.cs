namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents a variant attribute type for a product (e.g., Size, Color).
/// Each product can have multiple variant attributes that define the dimensions
/// of variation (e.g., a t-shirt might vary by Size and Color).
/// </summary>
public class ProductVariantAttribute
{
    /// <summary>
    /// Gets or sets the unique identifier for the variant attribute.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this attribute belongs to.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the attribute name (e.g., "Size", "Color").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display order for this attribute.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the attribute was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent product.
    /// </summary>
    public Domain.Entities.Product? Product { get; set; }

    /// <summary>
    /// Navigation property to the attribute values.
    /// </summary>
    public ICollection<ProductVariantAttributeValue> Values { get; set; } = new List<ProductVariantAttributeValue>();
}
