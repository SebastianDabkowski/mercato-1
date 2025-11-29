namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents an audit log entry for admin actions.
/// </summary>
public class AdminAuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who performed the action.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action performed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of entity affected.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the affected entity.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional details about the action.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets when the action occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the action was performed (if available).
    /// </summary>
    public string? IpAddress { get; set; }
}
