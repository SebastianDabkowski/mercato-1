namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents an SLA tracking record for a case (return request or complaint).
/// Records creation time, deadlines, and breach status.
/// </summary>
public class SlaTrackingRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for the SLA tracking record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the case ID (ReturnRequest ID) being tracked.
    /// </summary>
    public Guid CaseId { get; set; }

    /// <summary>
    /// Gets or sets the case number for display purposes.
    /// </summary>
    public string CaseNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the case type (e.g., "Return", "Complaint").
    /// </summary>
    public string CaseType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store/seller ID associated with the case.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name for display purposes.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the case was created.
    /// </summary>
    public DateTimeOffset CaseCreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the deadline for the seller's first response.
    /// </summary>
    public DateTimeOffset FirstResponseDeadline { get; set; }

    /// <summary>
    /// Gets or sets the deadline for case resolution.
    /// </summary>
    public DateTimeOffset ResolutionDeadline { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the seller first responded.
    /// Null if no response yet.
    /// </summary>
    public DateTimeOffset? FirstResponseAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the case was resolved.
    /// Null if not resolved yet.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the first response SLA was breached.
    /// </summary>
    public bool IsFirstResponseBreached { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the resolution SLA was breached.
    /// </summary>
    public bool IsResolutionBreached { get; set; }

    /// <summary>
    /// Gets or sets the overall SLA status.
    /// </summary>
    public SlaStatus Status { get; set; } = SlaStatus.Pending;

    /// <summary>
    /// Gets or sets the date and time when this record was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets the response time in hours if the seller has responded.
    /// </summary>
    public double? ResponseTimeHours => FirstResponseAt.HasValue
        ? (FirstResponseAt.Value - CaseCreatedAt).TotalHours
        : null;

    /// <summary>
    /// Gets the resolution time in hours if the case has been resolved.
    /// </summary>
    public double? ResolutionTimeHours => ResolvedAt.HasValue
        ? (ResolvedAt.Value - CaseCreatedAt).TotalHours
        : null;
}

/// <summary>
/// Represents the overall SLA status for a case.
/// </summary>
public enum SlaStatus
{
    /// <summary>
    /// Case is pending seller action.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Seller has responded within SLA.
    /// </summary>
    Responded = 1,

    /// <summary>
    /// Case is resolved within SLA.
    /// </summary>
    ResolvedWithinSla = 2,

    /// <summary>
    /// First response SLA has been breached.
    /// </summary>
    FirstResponseBreached = 3,

    /// <summary>
    /// Resolution SLA has been breached.
    /// </summary>
    ResolutionBreached = 4,

    /// <summary>
    /// Case is closed (resolved or rejected).
    /// </summary>
    Closed = 5
}
