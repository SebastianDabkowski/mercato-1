using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of calculating and updating a seller's reputation.
/// </summary>
public class CalculateReputationResult
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
    /// Gets the calculated reputation score (0-100).
    /// </summary>
    public decimal? ReputationScore { get; private init; }

    /// <summary>
    /// Gets the determined reputation level.
    /// </summary>
    public ReputationLevel ReputationLevel { get; private init; }

    /// <summary>
    /// Creates a successful result with the calculated reputation.
    /// </summary>
    /// <param name="reputationScore">The calculated reputation score.</param>
    /// <param name="reputationLevel">The determined reputation level.</param>
    /// <returns>A successful result.</returns>
    public static CalculateReputationResult Success(decimal? reputationScore, ReputationLevel reputationLevel) => new()
    {
        Succeeded = true,
        Errors = [],
        ReputationScore = reputationScore,
        ReputationLevel = reputationLevel
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CalculateReputationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors,
        ReputationScore = null,
        ReputationLevel = ReputationLevel.Unrated
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CalculateReputationResult Failure(string error) => Failure([error]);
}
