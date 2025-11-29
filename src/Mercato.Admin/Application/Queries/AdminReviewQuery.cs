using Mercato.Orders.Domain.Entities;

namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query parameters for filtering product reviews for admin moderation.
/// </summary>
public class AdminReviewFilterQuery
{
    /// <summary>
    /// Gets or sets the search term to filter by review text or buyer ID.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the statuses to filter by (optional). If empty, all statuses are included.
    /// </summary>
    public IReadOnlyList<ReviewStatus> Statuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the start date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? ToDate { get; set; }

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
/// Represents a summary of a product review for the admin list view.
/// </summary>
public class AdminReviewSummary
{
    /// <summary>
    /// Gets or sets the review ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID being reviewed.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that sold the product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (alias for privacy).
    /// </summary>
    public string BuyerAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating from 1 to 5 stars.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets a preview of the review text (truncated).
    /// </summary>
    public string ReviewTextPreview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the review.
    /// </summary>
    public ReviewStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the review was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the review was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}

/// <summary>
/// Represents detailed information about a product review for admin moderation.
/// </summary>
public class AdminReviewDetails
{
    /// <summary>
    /// Gets or sets the review ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order item ID.
    /// </summary>
    public Guid SellerSubOrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID being reviewed.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that sold the product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (alias for privacy).
    /// </summary>
    public string BuyerAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating from 1 to 5 stars.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets the full review text content.
    /// </summary>
    public string ReviewText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the review.
    /// </summary>
    public ReviewStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the review was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the review was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}

/// <summary>
/// Result of a filtered admin reviews query.
/// </summary>
public class GetAdminReviewsResult
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
    /// Gets the list of review summaries for the current page.
    /// </summary>
    public IReadOnlyList<AdminReviewSummary> Reviews { get; private init; } = [];

    /// <summary>
    /// Gets the total number of reviews matching the filter criteria.
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
    /// <param name="reviews">The review summaries for the current page.</param>
    /// <param name="totalCount">The total number of reviews matching the filter.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetAdminReviewsResult Success(IReadOnlyList<AdminReviewSummary> reviews, int totalCount, int page, int pageSize) => new()
    {
        Succeeded = true,
        Errors = [],
        Reviews = reviews,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminReviewsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminReviewsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetAdminReviewsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access reviews."]
    };
}

/// <summary>
/// Result of an admin review details query.
/// </summary>
public class GetAdminReviewDetailsResult
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
    /// Gets the review details.
    /// </summary>
    public AdminReviewDetails? ReviewDetails { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="reviewDetails">The review details.</param>
    /// <returns>A successful result.</returns>
    public static GetAdminReviewDetailsResult Success(AdminReviewDetails reviewDetails) => new()
    {
        Succeeded = true,
        Errors = [],
        ReviewDetails = reviewDetails
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminReviewDetailsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminReviewDetailsResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetAdminReviewDetailsResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access review details."]
    };
}
