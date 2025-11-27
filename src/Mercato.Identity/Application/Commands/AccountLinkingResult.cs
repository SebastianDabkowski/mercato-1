namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of an account linking operation.
/// </summary>
public class AccountLinkingResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the social login was already linked.
    /// </summary>
    public bool WasAlreadyLinked { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AccountLinkingResult Success()
    {
        return new AccountLinkingResult
        {
            Succeeded = true
        };
    }

    /// <summary>
    /// Creates a result indicating the social login was already linked.
    /// </summary>
    public static AccountLinkingResult AlreadyLinked()
    {
        return new AccountLinkingResult
        {
            Succeeded = true,
            WasAlreadyLinked = true
        };
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public static AccountLinkingResult Failure(string errorMessage)
    {
        return new AccountLinkingResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a failed result indicating the user was not found.
    /// </summary>
    public static AccountLinkingResult UserNotFound()
    {
        return new AccountLinkingResult
        {
            Succeeded = false,
            ErrorMessage = "User not found."
        };
    }

    /// <summary>
    /// Creates a failed result indicating the user is not a buyer.
    /// </summary>
    public static AccountLinkingResult NotABuyer()
    {
        return new AccountLinkingResult
        {
            Succeeded = false,
            ErrorMessage = "Account linking is only available for buyers."
        };
    }

    /// <summary>
    /// Creates a failed result indicating the social login is not linked.
    /// </summary>
    public static AccountLinkingResult NotLinked()
    {
        return new AccountLinkingResult
        {
            Succeeded = false,
            ErrorMessage = "The specified social login is not linked to this account."
        };
    }
}
