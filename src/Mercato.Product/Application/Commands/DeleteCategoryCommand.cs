namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for deleting a product category.
/// </summary>
public class DeleteCategoryCommand
{
    /// <summary>
    /// Gets or sets the category ID to delete.
    /// </summary>
    public Guid CategoryId { get; set; }
}
