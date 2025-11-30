namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a security incident record.
/// </summary>
public class SecurityIncident
{
    /// <summary>
    /// Gets or sets the unique identifier for the security incident.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the incident was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; set; }

    /// <summary>
    /// Gets or sets the source of the incident (e.g., IP address hash, user ID, system component).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detection rule that triggered the incident.
    /// </summary>
    public string DetectionRule { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity of the incident.
    /// </summary>
    public SecurityIncidentSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the current status of the incident.
    /// </summary>
    public SecurityIncidentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets a description of the incident.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional details or context about the incident.
    /// Do not store unnecessary personal data here.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the resolution notes when the incident is resolved.
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the incident was resolved.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the person who resolved the incident.
    /// </summary>
    public string? ResolvedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the incident was created in the system.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the incident was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether alerts have been sent for this incident.
    /// </summary>
    public bool AlertsSent { get; set; }
}
