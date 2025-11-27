namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents an audit log entry for user role change events.
/// </summary>
public class RoleChangeAuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user whose role was changed.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user whose role was changed.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous role(s) of the user.
    /// </summary>
    public string OldRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new role assigned to the user.
    /// </summary>
    public string NewRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the admin who performed the role change.
    /// </summary>
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the role change was performed.
    /// </summary>
    public DateTimeOffset PerformedAt { get; set; }

    /// <summary>
    /// Gets or sets additional context or details about the role change.
    /// </summary>
    public string Details { get; set; } = string.Empty;
}
