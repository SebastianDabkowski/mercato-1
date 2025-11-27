namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of a KYC approval operation.
/// </summary>
public class ApproveKycResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static ApproveKycResult Success() => new() { Succeeded = true };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ApproveKycResult Failure(string error) => new() { Succeeded = false, Errors = [error] };

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static ApproveKycResult Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList().AsReadOnly() };
}
