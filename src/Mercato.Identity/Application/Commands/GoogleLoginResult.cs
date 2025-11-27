namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a Google OAuth login attempt for a buyer.
/// </summary>
public class GoogleLoginResult
{
    /// <summary>
    /// Gets a value indicating whether the login was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the user ID of the authenticated buyer.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the email of the authenticated buyer.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the error message if login failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a new user registration.
    /// </summary>
    public bool IsNewUser { get; init; }

    /// <summary>
    /// Creates a successful login result for an existing user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="email">The user's email.</param>
    public static GoogleLoginResult Success(string userId, string email)
    {
        return new GoogleLoginResult
        {
            Succeeded = true,
            UserId = userId,
            Email = email,
            IsNewUser = false
        };
    }

    /// <summary>
    /// Creates a successful login result for a newly registered user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="email">The user's email.</param>
    public static GoogleLoginResult NewUserCreated(string userId, string email)
    {
        return new GoogleLoginResult
        {
            Succeeded = true,
            UserId = userId,
            Email = email,
            IsNewUser = true
        };
    }

    /// <summary>
    /// Creates a failed login result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public static GoogleLoginResult Failure(string errorMessage)
    {
        return new GoogleLoginResult
        {
            Succeeded = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a failed login result indicating the user is not a buyer.
    /// </summary>
    public static GoogleLoginResult NotABuyer()
    {
        return new GoogleLoginResult
        {
            Succeeded = false,
            ErrorMessage = "This login page is for buyers only. The account associated with this Google account has a different role."
        };
    }
}
