namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a buyer registration attempt.
/// </summary>
public class RegisterBuyerResult
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
    public static RegisterBuyerResult Success()
    {
        return new RegisterBuyerResult { Succeeded = true };
    }

    /// <summary>
    /// Creates a failed registration result with the specified errors.
    /// </summary>
    /// <param name="errors">The collection of error messages.</param>
    public static RegisterBuyerResult Failure(IEnumerable<string> errors)
    {
        return new RegisterBuyerResult
        {
            Succeeded = false,
            Errors = errors.ToList().AsReadOnly()
        };
    }

    /// <summary>
    /// Creates a failed registration result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static RegisterBuyerResult Failure(string error)
    {
        return new RegisterBuyerResult
        {
            Succeeded = false,
            Errors = new List<string> { error }.AsReadOnly()
        };
    }
}
