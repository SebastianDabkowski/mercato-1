using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Commands;

/// <summary>
/// Command for submitting a product review.
/// </summary>
public class SubmitProductReviewCommand
{
    /// <summary>
    /// Gets or sets the seller sub-order item ID to review.
    /// </summary>
    public Guid SellerSubOrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID who is submitting the review.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating from 1 to 5 stars.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets the review text content (max 2000 characters).
    /// </summary>
    public string ReviewText { get; set; } = string.Empty;
}

/// <summary>
/// Result of submitting a product review.
/// </summary>
public class SubmitProductReviewResult
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
    /// Gets the ID of the created product review.
    /// </summary>
    public Guid? ReviewId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="reviewId">The ID of the created review.</param>
    /// <returns>A successful result.</returns>
    public static SubmitProductReviewResult Success(Guid reviewId) => new()
    {
        Succeeded = true,
        Errors = [],
        ReviewId = reviewId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static SubmitProductReviewResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SubmitProductReviewResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static SubmitProductReviewResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to submit a review for this item."]
    };
}

/// <summary>
/// Result of getting product reviews.
/// </summary>
public class GetProductReviewsResult
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
    /// Gets the list of product reviews.
    /// </summary>
    public IReadOnlyList<ProductReview> Reviews { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="reviews">The product reviews.</param>
    /// <returns>A successful result.</returns>
    public static GetProductReviewsResult Success(IReadOnlyList<ProductReview> reviews) => new()
    {
        Succeeded = true,
        Errors = [],
        Reviews = reviews
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetProductReviewsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetProductReviewsResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of checking if a review can be submitted.
/// </summary>
public class CanSubmitReviewResult
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
    /// Gets a value indicating whether a review can be submitted.
    /// </summary>
    public bool CanSubmit { get; private init; }

    /// <summary>
    /// Gets the reason why a review cannot be submitted (if applicable).
    /// </summary>
    public string? BlockedReason { get; private init; }

    /// <summary>
    /// Creates a successful result indicating review can be submitted.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static CanSubmitReviewResult Yes() => new()
    {
        Succeeded = true,
        Errors = [],
        CanSubmit = true
    };

    /// <summary>
    /// Creates a successful result indicating review cannot be submitted.
    /// </summary>
    /// <param name="reason">The reason why review cannot be submitted.</param>
    /// <returns>A successful result.</returns>
    public static CanSubmitReviewResult No(string reason) => new()
    {
        Succeeded = true,
        Errors = [],
        CanSubmit = false,
        BlockedReason = reason
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CanSubmitReviewResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CanSubmitReviewResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CanSubmitReviewResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to check review eligibility for this item."]
    };
}

/// <summary>
/// Query for getting paginated product reviews.
/// </summary>
public class GetProductReviewsQuery
{
    /// <summary>
    /// Gets or sets the product ID to get reviews for.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 10.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the sort option. Defaults to Newest.
    /// </summary>
    public ReviewSortOption SortBy { get; set; } = ReviewSortOption.Newest;
}

/// <summary>
/// Result of getting paginated product reviews.
/// </summary>
public class GetProductReviewsPagedResult
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
    /// Gets the list of product reviews for the current page.
    /// </summary>
    public IReadOnlyList<ProductReview> Reviews { get; private init; } = [];

    /// <summary>
    /// Gets the total count of reviews for the product.
    /// </summary>
    public int TotalCount { get; private init; }

    /// <summary>
    /// Gets the average rating for the product. Null if no reviews exist.
    /// </summary>
    public double? AverageRating { get; private init; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int Page { get; private init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; private init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; private init; }

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage { get; private init; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="reviews">The product reviews for the current page.</param>
    /// <param name="totalCount">The total count of reviews.</param>
    /// <param name="averageRating">The average rating.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetProductReviewsPagedResult Success(
        IReadOnlyList<ProductReview> reviews,
        int totalCount,
        double? averageRating,
        int page,
        int pageSize)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        return new GetProductReviewsPagedResult
        {
            Succeeded = true,
            Errors = [],
            Reviews = reviews,
            TotalCount = totalCount,
            AverageRating = averageRating,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetProductReviewsPagedResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetProductReviewsPagedResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Command for reporting a product review.
/// </summary>
public class ReportReviewCommand
{
    /// <summary>
    /// Gets or sets the review ID to report.
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// Gets or sets the reporter (buyer) ID.
    /// </summary>
    public string ReporterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the report.
    /// </summary>
    public ReportReason Reason { get; set; }

    /// <summary>
    /// Gets or sets additional details about the report (optional, max 1000 characters).
    /// </summary>
    public string? AdditionalDetails { get; set; }
}

/// <summary>
/// Result of reporting a product review.
/// </summary>
public class ReportReviewResult
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
    /// Gets the ID of the created review report.
    /// </summary>
    public Guid? ReportId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="reportId">The ID of the created report.</param>
    /// <returns>A successful result.</returns>
    public static ReportReviewResult Success(Guid reportId) => new()
    {
        Succeeded = true,
        Errors = [],
        ReportId = reportId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ReportReviewResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ReportReviewResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ReportReviewResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to report this review."]
    };
}
