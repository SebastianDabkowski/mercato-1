namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a buyer login attempt.
/// </summary>
public class LoginBuyerResult
{
    /// <summary>
    /// Gets a value indicating whether the login was successful.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the account is locked out.
    /// </summary>
    public bool IsLockedOut { get; init; }

    /// <summary>
    /// Gets a value indicating whether two-factor authentication is required.
    /// </summary>
    public bool RequiresTwoFactor { get; init; }

    /// <summary>
    /// Gets a value indicating whether the account is not allowed (e.g., not confirmed).
    /// </summary>
    public bool IsNotAllowed { get; init; }

    /// <summary>
    /// Gets the error message if login failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful login result.
    /// </summary>
    public static LoginBuyerResult Success()
    {
        return new LoginBuyerResult { Succeeded = true };
    }

    /// <summary>
    /// Creates a failed login result with a lockout status.
    /// </summary>
    public static LoginBuyerResult LockedOut()
    {
        return new LoginBuyerResult
        {
            Succeeded = false,
            IsLockedOut = true,
            ErrorMessage = "Your account has been locked due to too many failed login attempts. Please try again later."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating two-factor authentication is required.
    /// </summary>
    public static LoginBuyerResult TwoFactorRequired()
    {
        return new LoginBuyerResult
        {
            Succeeded = false,
            RequiresTwoFactor = true,
            ErrorMessage = "Two-factor authentication is required."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating the account is not allowed.
    /// </summary>
    public static LoginBuyerResult NotAllowed()
    {
        return new LoginBuyerResult
        {
            Succeeded = false,
            IsNotAllowed = true,
            ErrorMessage = "Your account is not allowed to sign in."
        };
    }

    /// <summary>
    /// Creates a failed login result with invalid credentials.
    /// </summary>
    public static LoginBuyerResult InvalidCredentials()
    {
        return new LoginBuyerResult
        {
            Succeeded = false,
            ErrorMessage = "Invalid email or password."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating user is not a buyer.
    /// </summary>
    public static LoginBuyerResult NotABuyer()
    {
        return new LoginBuyerResult
        {
            Succeeded = false,
            ErrorMessage = "This login page is for buyers only. Please use the appropriate login for your account type."
        };
    }
}
