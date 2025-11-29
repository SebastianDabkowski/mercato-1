using Mercato.Orders.Domain.Entities;

namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Command for moderating a product review (approve, reject, or change visibility).
/// </summary>
public class ModerateReviewCommand
{
    /// <summary>
    /// Gets or sets the review ID to moderate.
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID making the moderation decision.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new status for the review.
    /// </summary>
    public ReviewStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the reason for the moderation decision.
    /// </summary>
    public string ModerationReason { get; set; } = string.Empty;
}

/// <summary>
/// Result of a review moderation operation.
/// </summary>
public class ModerateReviewResult
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
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static ModerateReviewResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ModerateReviewResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ModerateReviewResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static ModerateReviewResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to moderate reviews."]
    };
}
