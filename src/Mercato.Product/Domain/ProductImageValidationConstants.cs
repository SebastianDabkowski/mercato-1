namespace Mercato.Product.Domain;

/// <summary>
/// Validation constants for product image uploads.
/// </summary>
public static class ProductImageValidationConstants
{
    /// <summary>
    /// Maximum file size in bytes (5MB).
    /// </summary>
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum number of images allowed per product.
    /// </summary>
    public const int MaxImagesPerProduct = 10;

    /// <summary>
    /// Allowed MIME content types for image uploads.
    /// </summary>
    public static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    /// <summary>
    /// Allowed file extensions for image uploads.
    /// </summary>
    public static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    /// <summary>
    /// Width of generated thumbnail images in pixels.
    /// </summary>
    public const int ThumbnailWidth = 150;

    /// <summary>
    /// Height of generated thumbnail images in pixels.
    /// </summary>
    public const int ThumbnailHeight = 150;

    /// <summary>
    /// Maximum width of optimized images in pixels.
    /// </summary>
    public const int OptimizedMaxWidth = 1200;

    /// <summary>
    /// Maximum height of optimized images in pixels.
    /// </summary>
    public const int OptimizedMaxHeight = 1200;

    /// <summary>
    /// JPEG compression quality for optimized images (0-100).
    /// </summary>
    public const int JpegQuality = 85;

    /// <summary>
    /// Maximum length for file name storage.
    /// </summary>
    public const int FileNameMaxLength = 255;

    /// <summary>
    /// Maximum length for storage path.
    /// </summary>
    public const int StoragePathMaxLength = 500;

    /// <summary>
    /// Maximum length for content type.
    /// </summary>
    public const int ContentTypeMaxLength = 50;
}
