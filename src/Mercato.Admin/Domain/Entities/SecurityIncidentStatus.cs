namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Defines the status of a security incident.
/// </summary>
public enum SecurityIncidentStatus
{
    /// <summary>
    /// Incident has been created but not yet reviewed.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Incident has been triaged and initial assessment completed.
    /// </summary>
    Triaged = 1,

    /// <summary>
    /// Incident is under active investigation.
    /// </summary>
    InInvestigation = 2,

    /// <summary>
    /// Incident has been contained and mitigation is in progress.
    /// </summary>
    Contained = 3,

    /// <summary>
    /// Incident has been resolved.
    /// </summary>
    Resolved = 4,

    /// <summary>
    /// Incident was closed as a false positive.
    /// </summary>
    FalsePositive = 5
}
