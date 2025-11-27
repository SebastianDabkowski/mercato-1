namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents an authentication event (login, failed login, lockout, password reset, etc.).
/// </summary>
public class AuthenticationEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for the authentication event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of authentication event.
    /// </summary>
    public AuthenticationEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with this event (if known).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the email address used in the authentication attempt.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's role at the time of the event (e.g., Buyer, Seller, Admin).
    /// </summary>
    public string? UserRole { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the authentication attempt was made.
    /// Stored in hashed/anonymized format to protect privacy.
    /// </summary>
    public string? IpAddressHash { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from the request.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets whether the authentication event was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the failure reason if the event was not successful.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>
/// Defines the types of authentication events that can be logged.
/// </summary>
public enum AuthenticationEventType
{
    /// <summary>
    /// User login attempt.
    /// </summary>
    Login = 0,

    /// <summary>
    /// User logout.
    /// </summary>
    Logout = 1,

    /// <summary>
    /// Account lockout due to failed attempts.
    /// </summary>
    Lockout = 2,

    /// <summary>
    /// Password reset request.
    /// </summary>
    PasswordReset = 3,

    /// <summary>
    /// Password change.
    /// </summary>
    PasswordChange = 4,

    /// <summary>
    /// Two-factor authentication.
    /// </summary>
    TwoFactorAuthentication = 5
}
