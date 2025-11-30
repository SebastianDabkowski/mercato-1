using System.ComponentModel.DataAnnotations;
using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for updating an existing category attribute.
/// </summary>
public class UpdateCategoryAttributeCommand
{
    /// <summary>
    /// Gets or sets the attribute ID.
    /// </summary>
    [Required(ErrorMessage = "Attribute ID is required.")]
    public Guid AttributeId { get; set; }

    /// <summary>
    /// Gets or sets the attribute name.
    /// </summary>
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attribute type.
    /// </summary>
    public CategoryAttributeType Type { get; set; } = CategoryAttributeType.Text;

    /// <summary>
    /// Gets or sets a value indicating whether this attribute is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this attribute is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the list options as a JSON array.
    /// Only applicable when Type is List.
    /// </summary>
    [StringLength(4000, ErrorMessage = "List options cannot exceed 4000 characters.")]
    public string? ListOptions { get; set; }

    /// <summary>
    /// Gets or sets the display order for this attribute.
    /// </summary>
    public int DisplayOrder { get; set; }
}
