using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Commands;

/// <summary>
/// Command for submitting a seller rating.
/// </summary>
public class SubmitSellerRatingCommand
{
    /// <summary>
    /// Gets or sets the seller sub-order ID to rate.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID who is submitting the rating.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating from 1 to 5 stars.
    /// </summary>
    public int Rating { get; set; }
}

/// <summary>
/// Result of submitting a seller rating.
/// </summary>
public class SubmitSellerRatingResult
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
    /// Gets the ID of the created seller rating.
    /// </summary>
    public Guid? RatingId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="ratingId">The ID of the created rating.</param>
    /// <returns>A successful result.</returns>
    public static SubmitSellerRatingResult Success(Guid ratingId) => new()
    {
        Succeeded = true,
        Errors = [],
        RatingId = ratingId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static SubmitSellerRatingResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SubmitSellerRatingResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static SubmitSellerRatingResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to submit a rating for this sub-order."]
    };
}

/// <summary>
/// Result of getting seller ratings.
/// </summary>
public class GetSellerRatingsResult
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
    /// Gets the list of seller ratings.
    /// </summary>
    public IReadOnlyList<SellerRating> Ratings { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="ratings">The seller ratings.</param>
    /// <returns>A successful result.</returns>
    public static GetSellerRatingsResult Success(IReadOnlyList<SellerRating> ratings) => new()
    {
        Succeeded = true,
        Errors = [],
        Ratings = ratings
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerRatingsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerRatingsResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of checking if a seller rating can be submitted.
/// </summary>
public class CanSubmitSellerRatingResult
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
    /// Gets a value indicating whether a rating can be submitted.
    /// </summary>
    public bool CanSubmit { get; private init; }

    /// <summary>
    /// Gets the reason why a rating cannot be submitted (if applicable).
    /// </summary>
    public string? BlockedReason { get; private init; }

    /// <summary>
    /// Creates a successful result indicating rating can be submitted.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static CanSubmitSellerRatingResult Yes() => new()
    {
        Succeeded = true,
        Errors = [],
        CanSubmit = true
    };

    /// <summary>
    /// Creates a successful result indicating rating cannot be submitted.
    /// </summary>
    /// <param name="reason">The reason why rating cannot be submitted.</param>
    /// <returns>A successful result.</returns>
    public static CanSubmitSellerRatingResult No(string reason) => new()
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
    public static CanSubmitSellerRatingResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CanSubmitSellerRatingResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CanSubmitSellerRatingResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to check rating eligibility for this sub-order."]
    };
}

/// <summary>
/// Result of getting average rating for a store.
/// </summary>
public class GetAverageRatingResult
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
    /// Gets the average rating for the store. Null if no ratings exist.
    /// </summary>
    public double? AverageRating { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="averageRating">The average rating.</param>
    /// <returns>A successful result.</returns>
    public static GetAverageRatingResult Success(double? averageRating) => new()
    {
        Succeeded = true,
        Errors = [],
        AverageRating = averageRating
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetAverageRatingResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetAverageRatingResult Failure(string error) => Failure([error]);
}
