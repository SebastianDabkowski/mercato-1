using Mercato.Product.Domain.Entities;

namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query parameters for filtering photos in the moderation queue.
/// </summary>
public class PhotoModerationFilterQuery
{
    /// <summary>
    /// Gets or sets the store ID to filter by (optional).
    /// </summary>
    public Guid? StoreId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show only flagged photos.
    /// </summary>
    public bool FlaggedOnly { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Represents a summary of a photo for the admin moderation list view.
/// </summary>
public class PhotoModerationSummary
{
    /// <summary>
    /// Gets or sets the image ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this image belongs to.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID that owns the product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage path for the image.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the thumbnail path for the image.
    /// </summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// Gets or sets the original filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this photo is flagged.
    /// </summary>
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Gets or sets the reason why the photo was flagged.
    /// </summary>
    public string? FlagReason { get; set; }

    /// <summary>
    /// Gets or sets the moderation status.
    /// </summary>
    public PhotoModerationStatus ModerationStatus { get; set; }

    /// <summary>
    /// Gets or sets when the photo was uploaded.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the photo was flagged (if applicable).
    /// </summary>
    public DateTimeOffset? FlaggedAt { get; set; }
}

/// <summary>
/// Represents detailed information about a photo for admin moderation.
/// </summary>
public class PhotoModerationDetails
{
    /// <summary>
    /// Gets or sets the image ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this image belongs to.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID that owns the product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage path for the image.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the thumbnail path for the image.
    /// </summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// Gets or sets the optimized path for the image.
    /// </summary>
    public string? OptimizedPath { get; set; }

    /// <summary>
    /// Gets or sets the original filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets whether this is the main image for the product.
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this photo is flagged.
    /// </summary>
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Gets or sets the reason why the photo was flagged.
    /// </summary>
    public string? FlagReason { get; set; }

    /// <summary>
    /// Gets or sets when the photo was flagged.
    /// </summary>
    public DateTimeOffset? FlaggedAt { get; set; }

    /// <summary>
    /// Gets or sets the moderation status.
    /// </summary>
    public PhotoModerationStatus ModerationStatus { get; set; }

    /// <summary>
    /// Gets or sets the moderation reason (for removed photos).
    /// </summary>
    public string? ModerationReason { get; set; }

    /// <summary>
    /// Gets or sets when the photo was last moderated.
    /// </summary>
    public DateTimeOffset? ModeratedAt { get; set; }

    /// <summary>
    /// Gets or sets who last moderated the photo.
    /// </summary>
    public string? ModeratedBy { get; set; }

    /// <summary>
    /// Gets or sets when the photo was uploaded.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the moderation history for this photo.
    /// </summary>
    public IReadOnlyList<PhotoModerationHistoryEntry> ModerationHistory { get; set; } = [];

    /// <summary>
    /// Gets or sets the count of other images for this product.
    /// </summary>
    public int OtherProductImagesCount { get; set; }
}

/// <summary>
/// Represents a single entry in the photo moderation history.
/// </summary>
public class PhotoModerationHistoryEntry
{
    /// <summary>
    /// Gets or sets the decision ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who made the decision.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decision made (Approved or Removed).
    /// </summary>
    public PhotoModerationStatus Decision { get; set; }

    /// <summary>
    /// Gets or sets the reason for the decision.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the previous moderation status.
    /// </summary>
    public PhotoModerationStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets when the decision was made.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Result of a filtered photo moderation query.
/// </summary>
public class GetPhotosForModerationResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the list of photo summaries for the current page.
    /// </summary>
    public IReadOnlyList<PhotoModerationSummary> Photos { get; private init; } = [];

    /// <summary>
    /// Gets the total number of photos matching the filter criteria.
    /// </summary>
    public int TotalCount { get; private init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; private init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; private init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="photos">The photo summaries for the current page.</param>
    /// <param name="totalCount">The total number of photos matching the filter.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetPhotosForModerationResult Success(
        IReadOnlyList<PhotoModerationSummary> photos,
        int totalCount,
        int page,
        int pageSize) => new()
    {
        Succeeded = true,
        Errors = [],
        Photos = photos,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetPhotosForModerationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetPhotosForModerationResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetPhotosForModerationResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access photo moderation."]
    };
}

/// <summary>
/// Result of a photo moderation details query.
/// </summary>
public class GetPhotoModerationDetailsResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the photo moderation details.
    /// </summary>
    public PhotoModerationDetails? PhotoDetails { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="photoDetails">The photo moderation details.</param>
    /// <returns>A successful result.</returns>
    public static GetPhotoModerationDetailsResult Success(PhotoModerationDetails photoDetails) => new()
    {
        Succeeded = true,
        Errors = [],
        PhotoDetails = photoDetails
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetPhotoModerationDetailsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetPhotoModerationDetailsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetPhotoModerationDetailsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access photo moderation details."]
    };
}
