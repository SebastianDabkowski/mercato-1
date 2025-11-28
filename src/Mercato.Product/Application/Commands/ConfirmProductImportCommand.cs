namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for confirming and executing a product import.
/// </summary>
public class ConfirmProductImportCommand
{
    /// <summary>
    /// Gets or sets the import job ID to confirm.
    /// </summary>
    public Guid ImportJobId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this import.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID confirming the import.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;
}
