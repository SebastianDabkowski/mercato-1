using System.ComponentModel.DataAnnotations;

namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for updating an existing product category.
/// </summary>
public class UpdateCategoryCommand
{
    /// <summary>
    /// Gets or sets the category ID to update.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent category ID.
    /// Null for root categories.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the display order for sorting siblings.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Display order cannot be negative.")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the category is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
