namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Result of a KYC submission operation.
/// </summary>
public class SubmitKycResult
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
    /// Gets the ID of the created KYC submission (if successful).
    /// </summary>
    public Guid? SubmissionId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="submissionId">The ID of the created submission.</param>
    /// <returns>A successful result.</returns>
    public static SubmitKycResult Success(Guid submissionId) => new()
    {
        Succeeded = true,
        SubmissionId = submissionId,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static SubmitKycResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors,
        SubmissionId = null
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static SubmitKycResult Failure(string error) => Failure([error]);
}
