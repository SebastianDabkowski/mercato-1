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
