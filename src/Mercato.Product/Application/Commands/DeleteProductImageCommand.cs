namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for deleting a product image.
/// </summary>
public class DeleteProductImageCommand
{
    /// <summary>
    /// Gets or sets the image ID to delete.
    /// </summary>
    public Guid ImageId { get; set; }

    /// <summary>
    /// Gets or sets the product ID the image belongs to.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns the product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID performing the deletion.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;
}
