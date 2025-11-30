namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents an attribute template defined for a category.
/// When a seller creates a product in this category, these attributes
/// are presented as structured fields for data entry.
/// </summary>
public class CategoryAttribute
{
    /// <summary>
    /// Gets or sets the unique identifier for the category attribute.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the category ID this attribute belongs to.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the attribute name (e.g., "Brand", "Material", "Screen Size").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attribute type (Text, Number, or List).
    /// </summary>
    public CategoryAttributeType Type { get; set; } = CategoryAttributeType.Text;

    /// <summary>
    /// Gets or sets a value indicating whether this attribute is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this attribute is deprecated.
    /// Deprecated attributes are hidden from new product creation but remain
    /// visible for existing products and reports.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the list of available options as a JSON array.
    /// Only applicable when Type is List.
    /// Format: ["Option1", "Option2", "Option3"]
    /// </summary>
    public string? ListOptions { get; set; }

    /// <summary>
    /// Gets or sets the display order for this attribute within its category.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the attribute was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the attribute was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent category.
    /// </summary>
    public Category? Category { get; set; }
}
