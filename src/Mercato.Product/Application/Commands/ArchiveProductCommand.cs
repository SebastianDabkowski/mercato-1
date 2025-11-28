namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for archiving (soft-deleting) a product.
/// </summary>
public class ArchiveProductCommand
{
    /// <summary>
    /// Gets or sets the product ID to archive.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID performing the archive (for authorization and audit).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID that owns this product (for authorization).
    /// </summary>
    public Guid StoreId { get; set; }
}
