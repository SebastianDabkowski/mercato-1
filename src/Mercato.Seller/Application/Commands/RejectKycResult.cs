namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of a KYC rejection operation.
/// </summary>
public class RejectKycResult
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
    public static RejectKycResult Success() => new() { Succeeded = true };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RejectKycResult Failure(string error) => new() { Succeeded = false, Errors = [error] };

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failed result.</returns>
    public static RejectKycResult Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList().AsReadOnly() };
}
