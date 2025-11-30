namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Defines the severity levels for security incidents.
/// </summary>
public enum SecurityIncidentSeverity
{
    /// <summary>
    /// Low severity - minor security events that require monitoring.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium severity - security events that require investigation.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High severity - significant security events requiring prompt response.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - severe security events requiring immediate response.
    /// </summary>
    Critical = 3
}
