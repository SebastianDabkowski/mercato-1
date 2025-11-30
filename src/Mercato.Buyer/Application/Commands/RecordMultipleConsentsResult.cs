namespace Mercato.Buyer.Application.Commands;

/// <summary>
/// Result of recording multiple consent decisions.
/// </summary>
public class RecordMultipleConsentsResult
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
    /// Gets the number of consents recorded.
    /// </summary>
    public int ConsentsRecorded { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="consentsRecorded">The number of consents recorded.</param>
    /// <returns>A successful result.</returns>
    public static RecordMultipleConsentsResult Success(int consentsRecorded) => new()
    {
        Succeeded = true,
        Errors = [],
        ConsentsRecorded = consentsRecorded
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static RecordMultipleConsentsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static RecordMultipleConsentsResult Failure(string error) => Failure([error]);
}
