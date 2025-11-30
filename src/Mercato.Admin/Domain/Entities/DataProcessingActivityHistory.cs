namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a historical record of changes made to a data processing activity.
/// This entity supports audit trail requirements for GDPR compliance.
/// </summary>
public class DataProcessingActivityHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for this history record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the related data processing activity.
    /// </summary>
    public Guid DataProcessingActivityId { get; set; }

    /// <summary>
    /// Gets or sets the type of change (e.g., Created, Updated, Deactivated).
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized previous values before the change.
    /// This will be null for creation records.
    /// </summary>
    public string? PreviousValues { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized new values after the change.
    /// </summary>
    public string NewValues { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the change was made.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who made the change.
    /// </summary>
    public string ChangedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user who made the change.
    /// </summary>
    public string? ChangedByUserEmail { get; set; }

    /// <summary>
    /// Gets or sets an optional reason or notes for the change.
    /// </summary>
    public string? Reason { get; set; }
}
