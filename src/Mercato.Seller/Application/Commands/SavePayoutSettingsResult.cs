namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of a save payout settings operation.
/// </summary>
public class SavePayoutSettingsResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static SavePayoutSettingsResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed result.</returns>
    public static SavePayoutSettingsResult Failure(IEnumerable<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors.ToList().AsReadOnly()
    };

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SavePayoutSettingsResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}
