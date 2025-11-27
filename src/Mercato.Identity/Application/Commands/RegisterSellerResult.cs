namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a seller registration attempt.
/// </summary>
public class RegisterSellerResult
{
    /// <summary>
    /// Gets a value indicating whether the registration was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the collection of error messages if registration failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful registration result.
    /// </summary>
    public static RegisterSellerResult Success()
    {
        return new RegisterSellerResult { Succeeded = true };
    }

    /// <summary>
    /// Creates a failed registration result with the specified errors.
    /// </summary>
    /// <param name="errors">The collection of error messages.</param>
    public static RegisterSellerResult Failure(IEnumerable<string> errors)
    {
        return new RegisterSellerResult
        {
            Succeeded = false,
            Errors = errors.ToList().AsReadOnly()
        };
    }

    /// <summary>
    /// Creates a failed registration result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static RegisterSellerResult Failure(string error)
    {
        return new RegisterSellerResult
        {
            Succeeded = false,
            Errors = new List<string> { error }.AsReadOnly()
        };
    }
}
