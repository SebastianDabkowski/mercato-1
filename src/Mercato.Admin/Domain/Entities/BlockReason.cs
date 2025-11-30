namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Defines the possible reasons for blocking a user account.
/// </summary>
public enum BlockReason
{
    /// <summary>
    /// User account blocked due to fraudulent activity.
    /// </summary>
    Fraud = 0,

    /// <summary>
    /// User account blocked due to spam behavior.
    /// </summary>
    Spam = 1,

    /// <summary>
    /// User account blocked due to policy violation.
    /// </summary>
    PolicyViolation = 2,

    /// <summary>
    /// User account blocked due to abuse of service.
    /// </summary>
    AbuseOfService = 3,

    /// <summary>
    /// User account blocked for other reasons.
    /// </summary>
    Other = 4
}
