namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Defines the possible account statuses for a user.
/// </summary>
public enum UserAccountStatus
{
    /// <summary>
    /// User account is active and can access the platform.
    /// </summary>
    Active = 0,

    /// <summary>
    /// User account is blocked and cannot access the platform.
    /// </summary>
    Blocked = 1,

    /// <summary>
    /// User account is pending email verification.
    /// </summary>
    PendingVerification = 2
}
