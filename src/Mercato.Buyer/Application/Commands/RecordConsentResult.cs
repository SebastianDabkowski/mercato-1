namespace Mercato.Buyer.Application.Commands;

/// <summary>
/// Result of recording a consent decision.
/// </summary>
public class RecordConsentResult
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
    /// Gets the ID of the recorded consent.
    /// </summary>
    public Guid? ConsentId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="consentId">The ID of the recorded consent.</param>
    /// <returns>A successful result.</returns>
    public static RecordConsentResult Success(Guid consentId) => new()
    {
        Succeeded = true,
        Errors = [],
        ConsentId = consentId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RecordConsentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RecordConsentResult Failure(string error) => Failure([error]);
}
