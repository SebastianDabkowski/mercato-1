namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Represents the result of an admin login attempt.
/// </summary>
public class LoginAdminResult
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
    /// Gets a value indicating whether the account is blocked by an admin.
    /// </summary>
    public bool IsBlocked { get; init; }

    /// <summary>
    /// Gets a value indicating whether two-factor authentication is required.
    /// Reserved for future use when two-factor authentication is implemented.
    /// </summary>
    public bool RequiresTwoFactor { get; init; }

    /// <summary>
    /// Gets the error message if login failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the authenticated user's ID. Only set when login is successful.
    /// Used for secure session token creation without requiring additional database lookups.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Creates a successful login result with the authenticated user's ID.
    /// </summary>
    /// <param name="userId">The authenticated user's ID for session token creation.</param>
    public static LoginAdminResult Success(string userId)
    {
        return new LoginAdminResult { Succeeded = true, UserId = userId };
    }

    /// <summary>
    /// Creates a failed login result with a lockout status.
    /// </summary>
    public static LoginAdminResult LockedOut()
    {
        return new LoginAdminResult
        {
            Succeeded = false,
            IsLockedOut = true,
            ErrorMessage = "Your account has been locked due to too many failed login attempts. Please contact support."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating two-factor authentication is required.
    /// Reserved for future use when two-factor authentication is implemented.
    /// </summary>
    public static LoginAdminResult TwoFactorRequired()
    {
        return new LoginAdminResult
        {
            Succeeded = false,
            RequiresTwoFactor = true,
            ErrorMessage = "Two-factor authentication is required."
        };
    }

    /// <summary>
    /// Creates a failed login result with invalid credentials.
    /// </summary>
    public static LoginAdminResult InvalidCredentials()
    {
        return new LoginAdminResult
        {
            Succeeded = false,
            ErrorMessage = "Invalid email or password."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating user is not an admin.
    /// </summary>
    public static LoginAdminResult NotAnAdmin()
    {
        return new LoginAdminResult
        {
            Succeeded = false,
            ErrorMessage = "This login page is for administrators only. Please use the appropriate login for your account type."
        };
    }

    /// <summary>
    /// Creates a failed login result indicating the account is blocked.
    /// </summary>
    public static LoginAdminResult Blocked()
    {
        return new LoginAdminResult
        {
            Succeeded = false,
            IsBlocked = true,
            ErrorMessage = "Your account has been blocked. Please contact support for assistance."
        };
    }
}
