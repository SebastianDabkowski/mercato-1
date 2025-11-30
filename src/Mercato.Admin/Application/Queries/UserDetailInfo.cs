namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Data transfer object containing detailed user account information for admin review.
/// </summary>
public class UserDetailInfo
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of roles assigned to the user.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets the user's account status.
    /// </summary>
    public UserAccountStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the user's last login.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets whether the user's email is confirmed.
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Gets or sets whether two-factor authentication is enabled.
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether the user's phone number is confirmed.
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; }

    /// <summary>
    /// Gets or sets the number of failed access attempts.
    /// </summary>
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// Gets or sets whether lockout is enabled for this user.
    /// </summary>
    public bool LockoutEnabled { get; set; }

    /// <summary>
    /// Gets or sets the lockout end date if the account is currently locked.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets the recent login activity for this user.
    /// </summary>
    public IReadOnlyList<LoginActivityInfo> RecentLoginActivity { get; set; } = [];

    /// <summary>
    /// Gets or sets any admin notes or flags for this user.
    /// </summary>
    public IReadOnlyList<string> AdminNotes { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the user is currently blocked.
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Gets or sets the email of the admin who blocked the user.
    /// </summary>
    public string? BlockedByAdminEmail { get; set; }

    /// <summary>
    /// Gets or sets when the user was blocked.
    /// </summary>
    public DateTimeOffset? BlockedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for blocking the user.
    /// </summary>
    public string? BlockReason { get; set; }

    /// <summary>
    /// Gets or sets optional additional details about the block reason.
    /// </summary>
    public string? BlockReasonDetails { get; set; }

    /// <summary>
    /// Gets or sets the full block/reactivate history for this user.
    /// </summary>
    public IReadOnlyList<BlockHistoryInfo> BlockHistory { get; set; } = [];
}

/// <summary>
/// Represents a login activity record for a user.
/// </summary>
public class LoginActivityInfo
{
    /// <summary>
    /// Gets or sets the date and time of the login attempt.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets whether the login was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the event type description.
    /// </summary>
    public string EventType { get; set; } = string.Empty;
}
