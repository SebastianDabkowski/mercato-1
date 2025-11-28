using System.ComponentModel.DataAnnotations;

namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for creating a new product category.
/// </summary>
public class CreateCategoryCommand
{
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
}
