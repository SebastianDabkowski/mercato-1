namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents SLA configuration settings for case handling.
/// SLA thresholds are stored centrally and editable by admins only.
/// </summary>
public class SlaConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for the SLA configuration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this SLA configuration (e.g., "Default", "Holiday").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the case type this configuration applies to.
    /// Null means it applies to all case types.
    /// </summary>
    public string? CaseType { get; set; }

    /// <summary>
    /// Gets or sets the category this configuration applies to.
    /// Null means it applies to all categories.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the number of hours allowed for first response.
    /// </summary>
    public int FirstResponseDeadlineHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the number of hours allowed for resolution.
    /// </summary>
    public int ResolutionDeadlineHours { get; set; } = 72;

    /// <summary>
    /// Gets or sets a value indicating whether this configuration is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority order for applying configurations.
    /// Lower numbers have higher priority.
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets the date and time when this configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this configuration.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this configuration was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this configuration.
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
