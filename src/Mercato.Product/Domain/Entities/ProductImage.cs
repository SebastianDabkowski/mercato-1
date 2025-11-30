namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents a product image in the marketplace catalog.
/// </summary>
public class ProductImage
{
    /// <summary>
    /// Gets or sets the unique identifier for the product image.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this image belongs to.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the original filename of the uploaded image.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server storage path for the original image.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME content type (e.g., image/jpeg, image/png, image/webp).
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets whether this is the main/primary image for the product.
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// Gets or sets the display order for this image.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the image was uploaded.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the path to the thumbnail version of the image.
    /// </summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the optimized/resized version of the image.
    /// </summary>
    public string? OptimizedPath { get; set; }

    /// <summary>
    /// Gets or sets the moderation status for admin review.
    /// </summary>
    public PhotoModerationStatus ModerationStatus { get; set; } = PhotoModerationStatus.PendingReview;

    /// <summary>
    /// Gets or sets the reason for removal when moderation status is Removed.
    /// </summary>
    public string? ModerationReason { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the photo was last moderated.
    /// </summary>
    public DateTimeOffset? ModeratedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the admin who last moderated this photo.
    /// </summary>
    public string? ModeratedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the photo was flagged for review.
    /// This can be set by automatic checks or user reports.
    /// </summary>
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Gets or sets the reason why the photo was flagged.
    /// </summary>
    public string? FlagReason { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the photo was flagged.
    /// </summary>
    public DateTimeOffset? FlaggedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent product.
    /// </summary>
    public Product? Product { get; set; }
}
