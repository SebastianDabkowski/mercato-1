namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a status change for a security incident, providing an audit trail.
/// </summary>
public class SecurityIncidentStatusChange
{
    /// <summary>
    /// Gets or sets the unique identifier for this status change record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the security incident this change belongs to.
    /// </summary>
    public Guid SecurityIncidentId { get; set; }

    /// <summary>
    /// Gets or sets the previous status of the incident.
    /// </summary>
    public SecurityIncidentStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status of the incident.
    /// </summary>
    public SecurityIncidentStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the person who made the status change.
    /// </summary>
    public string ChangedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the status change occurred.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the status change.
    /// </summary>
    public string? Notes { get; set; }
}
