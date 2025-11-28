namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for uploading a product import file.
/// </summary>
public class UploadProductImportCommand
{
    /// <summary>
    /// Gets or sets the store ID that owns this import.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID initiating the import.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file content stream.
    /// </summary>
    public Stream FileContent { get; set; } = Stream.Null;
}
