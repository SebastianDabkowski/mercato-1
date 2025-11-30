using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for security incident data access operations.
/// </summary>
public interface ISecurityIncidentRepository
{
    /// <summary>
    /// Adds a new security incident.
    /// </summary>
    /// <param name="incident">The security incident to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added security incident.</returns>
    Task<SecurityIncident> AddAsync(SecurityIncident incident, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a security incident by its ID.
    /// </summary>
    /// <param name="id">The incident ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The security incident, or null if not found.</returns>
    Task<SecurityIncident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing security incident.
    /// </summary>
    /// <param name="incident">The security incident to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated security incident.</returns>
    Task<SecurityIncident> UpdateAsync(SecurityIncident incident, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a status change record for a security incident.
    /// </summary>
    /// <param name="statusChange">The status change record to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added status change record.</returns>
    Task<SecurityIncidentStatusChange> AddStatusChangeAsync(SecurityIncidentStatusChange statusChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all status change records for a security incident.
    /// </summary>
    /// <param name="incidentId">The incident ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of status change records.</returns>
    Task<IReadOnlyList<SecurityIncidentStatusChange>> GetStatusChangesAsync(Guid incidentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security incidents with optional filtering.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="severity">Optional severity filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="detectionRule">Optional detection rule filter.</param>
    /// <param name="maxResults">Maximum number of results to return. Default is 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A filtered list of security incidents.</returns>
    Task<IReadOnlyList<SecurityIncident>> GetFilteredAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        SecurityIncidentSeverity? severity = null,
        SecurityIncidentStatus? status = null,
        string? detectionRule = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of incidents by status for a given time range.
    /// </summary>
    /// <param name="startDate">Start date filter.</param>
    /// <param name="endDate">End date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of status to count.</returns>
    Task<IDictionary<SecurityIncidentStatus, int>> GetIncidentCountsByStatusAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);
}
