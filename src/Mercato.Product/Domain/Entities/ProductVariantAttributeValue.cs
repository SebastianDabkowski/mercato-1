namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents a specific value for a variant attribute (e.g., "Small", "Medium", "Large" for Size).
/// </summary>
public class ProductVariantAttributeValue
{
    /// <summary>
    /// Gets or sets the unique identifier for the attribute value.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the variant attribute ID this value belongs to.
    /// </summary>
    public Guid VariantAttributeId { get; set; }

    /// <summary>
    /// Gets or sets the value name (e.g., "Small", "Red").
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display order for this value within its attribute.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the value was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent variant attribute.
    /// </summary>
    public ProductVariantAttribute? VariantAttribute { get; set; }
}
