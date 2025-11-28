namespace Mercato.Product.Application.Commands;

/// <summary>
/// Result of uploading a product image.
/// </summary>
public class UploadProductImageResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the failure was due to authorization.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the ID of the created image, if successful.
    /// </summary>
    public Guid? ImageId { get; private init; }

    /// <summary>
    /// Gets the URL path to the original image, if successful.
    /// </summary>
    public string? ImageUrl { get; private init; }

    /// <summary>
    /// Gets the URL path to the thumbnail image, if successful.
    /// </summary>
    public string? ThumbnailUrl { get; private init; }

    /// <summary>
    /// Gets the URL path to the optimized image, if successful.
    /// </summary>
    public string? OptimizedUrl { get; private init; }

    /// <summary>
    /// Creates a successful result with the image details.
    /// </summary>
    /// <param name="imageId">The ID of the created image.</param>
    /// <param name="imageUrl">The URL path to the original image.</param>
    /// <param name="thumbnailUrl">The URL path to the thumbnail image.</param>
    /// <param name="optimizedUrl">The URL path to the optimized image.</param>
    /// <returns>A successful result.</returns>
    public static UploadProductImageResult Success(Guid imageId, string imageUrl, string? thumbnailUrl, string? optimizedUrl) => new()
    {
        Succeeded = true,
        Errors = [],
        ImageId = imageId,
        ImageUrl = imageUrl,
        ThumbnailUrl = thumbnailUrl,
        OptimizedUrl = optimizedUrl
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UploadProductImageResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UploadProductImageResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result with the specified error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A not authorized result.</returns>
    public static UploadProductImageResult NotAuthorized(string error) => new()
    {
        Succeeded = false,
        Errors = [error],
        IsNotAuthorized = true
    };
}
