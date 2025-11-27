namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents an audit log entry for KYC submission events.
/// </summary>
public class KycAuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the associated KYC submission.
    /// </summary>
    public Guid KycSubmissionId { get; set; }

    /// <summary>
    /// Gets or sets the action performed (e.g., "Submitted", "StatusChanged", "Reviewed").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous status (if applicable).
    /// </summary>
    public KycStatus? OldStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status (if applicable).
    /// </summary>
    public KycStatus? NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the user ID who performed the action.
    /// </summary>
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the action was performed.
    /// </summary>
    public DateTimeOffset PerformedAt { get; set; }

    /// <summary>
    /// Gets or sets additional context or details about the action.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}
