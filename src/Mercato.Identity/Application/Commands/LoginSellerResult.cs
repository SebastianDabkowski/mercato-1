namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of a seller login attempt.
/// </summary>
public class LoginSellerResult
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
    /// Gets a value indicating whether the email has not been verified.
    /// </summary>
    public bool EmailNotVerified { get; init; }

    /// <summary>
    /// Gets the error message if login failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful login result.
    /// </summary>
    public static LoginSellerResult Success()
    {
        return new LoginSellerResult { Succeeded = true };
    }

    /// <summary>
    /// Creates a failed login result with a lockout status.
    /// </summary>
    public static LoginSellerResult LockedOut()
    {
        return new LoginSellerResult
        {
            Succeeded = false,
            IsLockedOut = true,
            ErrorMessage = "Your account has been locked due to too many failed login attempts. Please try again later."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating two-factor authentication is required.
    /// </summary>
    public static LoginSellerResult TwoFactorRequired()
    {
        return new LoginSellerResult
        {
            Succeeded = false,
            RequiresTwoFactor = true,
            ErrorMessage = "Two-factor authentication is required."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating the email has not been verified.
    /// </summary>
    public static LoginSellerResult UnverifiedEmail()
    {
        return new LoginSellerResult
        {
            Succeeded = false,
            EmailNotVerified = true,
            ErrorMessage = "Please verify your email address before logging in."
        };
    }

    /// <summary>
    /// Creates a failed login result with invalid credentials.
    /// </summary>
    public static LoginSellerResult InvalidCredentials()
    {
        return new LoginSellerResult
        {
            Succeeded = false,
            ErrorMessage = "Invalid email or password."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating user is not a seller.
    /// </summary>
    public static LoginSellerResult NotASeller()
    {
        return new LoginSellerResult
        {
            Succeeded = false,
            ErrorMessage = "This login page is for sellers only. Please use the appropriate login for your account type."
        };
    }
}
