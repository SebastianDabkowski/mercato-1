using System.ComponentModel.DataAnnotations;

namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for deleting a category attribute.
/// </summary>
public class DeleteCategoryAttributeCommand
{
    /// <summary>
    /// Gets or sets the attribute ID.
    /// </summary>
    [Required(ErrorMessage = "Attribute ID is required.")]
    public Guid AttributeId { get; set; }
}
