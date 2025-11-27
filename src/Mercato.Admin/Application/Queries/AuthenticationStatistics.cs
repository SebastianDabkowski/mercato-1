using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Contains aggregate statistics for authentication events.
/// </summary>
public class AuthenticationStatistics
{
    /// <summary>
    /// Gets or sets the start date of the statistics period.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the statistics period.
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Gets or sets the total number of successful logins.
    /// </summary>
    public int TotalSuccessfulLogins { get; set; }

    /// <summary>
    /// Gets or sets the total number of failed logins.
    /// </summary>
    public int TotalFailedLogins { get; set; }

    /// <summary>
    /// Gets or sets the total number of account lockouts.
    /// </summary>
    public int TotalLockouts { get; set; }

    /// <summary>
    /// Gets or sets the total number of password resets.
    /// </summary>
    public int TotalPasswordResets { get; set; }

    /// <summary>
    /// Gets or sets the event counts grouped by type.
    /// </summary>
    public Dictionary<AuthenticationEventType, int> EventsByType { get; set; } = new();
}

/// <summary>
/// Contains information about suspicious authentication activity.
/// </summary>
public class SuspiciousActivityInfo
{
    /// <summary>
    /// Gets or sets the type of suspicious activity detected.
    /// </summary>
    public SuspiciousActivityType ActivityType { get; set; }

    /// <summary>
    /// Gets or sets the description of the suspicious activity.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level of the alert.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the count associated with this activity (e.g., number of failed attempts).
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the identifier associated with the activity (e.g., IP hash or email).
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this suspicious activity was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; set; }
}

/// <summary>
/// Defines the types of suspicious authentication activity.
/// </summary>
public enum SuspiciousActivityType
{
    /// <summary>
    /// Multiple failed login attempts from the same IP address.
    /// </summary>
    BruteForce = 0,

    /// <summary>
    /// Rapid login attempts across multiple accounts.
    /// </summary>
    CredentialStuffing = 1,

    /// <summary>
    /// Multiple accounts accessed from the same IP.
    /// </summary>
    AccountHopping = 2,

    /// <summary>
    /// High volume of login attempts in a short time.
    /// </summary>
    RapidAttempts = 3
}

/// <summary>
/// Defines the severity levels for security alerts.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Low severity - informational.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity - should be reviewed.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity - requires attention.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - immediate action required.
    /// </summary>
    Critical = 3
}
