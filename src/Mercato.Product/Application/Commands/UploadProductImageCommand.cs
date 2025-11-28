using System.IO;

namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for uploading a product image.
/// </summary>
public class UploadProductImageCommand
{
    /// <summary>
    /// Gets or sets the product ID to upload the image for.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns the product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID performing the upload.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original filename of the uploaded file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME content type of the uploaded file.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the file stream containing the image data.
    /// </summary>
    public Stream? FileStream { get; set; }

    /// <summary>
    /// Gets or sets whether this image should be set as the main image.
    /// </summary>
    public bool SetAsMain { get; set; }
}
