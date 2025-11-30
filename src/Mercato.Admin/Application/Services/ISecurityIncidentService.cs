using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for security incident management operations.
/// </summary>
public interface ISecurityIncidentService
{
    /// <summary>
    /// Creates a new security incident when a detection rule triggers.
    /// </summary>
    /// <param name="source">The source of the incident.</param>
    /// <param name="detectionRule">The rule that detected the incident.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="description">Description of the incident.</param>
    /// <param name="details">Optional additional details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the created incident or errors.</returns>
    Task<CreateSecurityIncidentResult> CreateIncidentAsync(
        string source,
        string detectionRule,
        SecurityIncidentSeverity severity,
        string description,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a security incident.
    /// </summary>
    /// <param name="incidentId">The incident ID.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="userId">The user making the change.</param>
    /// <param name="notes">Optional notes about the status change.</param>
    /// <param name="resolutionNotes">Optional resolution notes if resolving.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<UpdateSecurityIncidentStatusResult> UpdateStatusAsync(
        Guid incidentId,
        SecurityIncidentStatus newStatus,
        string userId,
        string? notes = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a security incident by ID.
    /// </summary>
    /// <param name="incidentId">The incident ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the incident or errors.</returns>
    Task<GetSecurityIncidentResult> GetIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security incidents with optional filtering for compliance review.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="severity">Optional severity filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="detectionRule">Optional detection rule filter.</param>
    /// <param name="maxResults">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the filtered incidents.</returns>
    Task<GetSecurityIncidentsResult> GetIncidentsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        SecurityIncidentSeverity? severity = null,
        SecurityIncidentStatus? status = null,
        string? detectionRule = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status change history for an incident.
    /// </summary>
    /// <param name="incidentId">The incident ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the status change history.</returns>
    Task<GetStatusChangesResult> GetStatusHistoryAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a compliance report of incidents for a given time range.
    /// </summary>
    /// <param name="startDate">Start date for the report.</param>
    /// <param name="endDate">End date for the report.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the compliance report data.</returns>
    Task<GetComplianceReportResult> GetComplianceReportAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of the create security incident operation.
/// </summary>
public class CreateSecurityIncidentResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the created security incident.
    /// </summary>
    public SecurityIncident? Incident { get; init; }

    /// <summary>
    /// Gets a value indicating whether high-severity alerts were triggered.
    /// </summary>
    public bool AlertsTriggered { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="incident">The created incident.</param>
    /// <param name="alertsTriggered">Whether alerts were triggered.</param>
    /// <returns>A successful result.</returns>
    public static CreateSecurityIncidentResult Success(SecurityIncident incident, bool alertsTriggered = false) => new()
    {
        Succeeded = true,
        Incident = incident,
        AlertsTriggered = alertsTriggered,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static CreateSecurityIncidentResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static CreateSecurityIncidentResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}

/// <summary>
/// Result of the update security incident status operation.
/// </summary>
public class UpdateSecurityIncidentStatusResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the updated security incident.
    /// </summary>
    public SecurityIncident? Incident { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="incident">The updated incident.</param>
    /// <returns>A successful result.</returns>
    public static UpdateSecurityIncidentStatusResult Success(SecurityIncident incident) => new()
    {
        Succeeded = true,
        Incident = incident,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static UpdateSecurityIncidentStatusResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static UpdateSecurityIncidentStatusResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}

/// <summary>
/// Result of the get security incident operation.
/// </summary>
public class GetSecurityIncidentResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the security incident.
    /// </summary>
    public SecurityIncident? Incident { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="incident">The incident.</param>
    /// <returns>A successful result.</returns>
    public static GetSecurityIncidentResult Success(SecurityIncident incident) => new()
    {
        Succeeded = true,
        Incident = incident,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetSecurityIncidentResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}

/// <summary>
/// Result of the get security incidents operation.
/// </summary>
public class GetSecurityIncidentsResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the list of security incidents.
    /// </summary>
    public IReadOnlyList<SecurityIncident> Incidents { get; init; } = [];

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="incidents">The incidents.</param>
    /// <returns>A successful result.</returns>
    public static GetSecurityIncidentsResult Success(IReadOnlyList<SecurityIncident> incidents) => new()
    {
        Succeeded = true,
        Incidents = incidents,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetSecurityIncidentsResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}

/// <summary>
/// Result of the get status changes operation.
/// </summary>
public class GetStatusChangesResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the list of status changes.
    /// </summary>
    public IReadOnlyList<SecurityIncidentStatusChange> StatusChanges { get; init; } = [];

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="statusChanges">The status changes.</param>
    /// <returns>A successful result.</returns>
    public static GetStatusChangesResult Success(IReadOnlyList<SecurityIncidentStatusChange> statusChanges) => new()
    {
        Succeeded = true,
        StatusChanges = statusChanges,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetStatusChangesResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}

/// <summary>
/// Result of the get compliance report operation.
/// </summary>
public class GetComplianceReportResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the list of incidents in the report.
    /// </summary>
    public IReadOnlyList<SecurityIncident> Incidents { get; init; } = [];

    /// <summary>
    /// Gets the count of incidents by status.
    /// </summary>
    public IDictionary<SecurityIncidentStatus, int> IncidentsByStatus { get; init; } = new Dictionary<SecurityIncidentStatus, int>();

    /// <summary>
    /// Gets the start date of the report period.
    /// </summary>
    public DateTimeOffset StartDate { get; init; }

    /// <summary>
    /// Gets the end date of the report period.
    /// </summary>
    public DateTimeOffset EndDate { get; init; }

    /// <summary>
    /// Gets the total count of incidents in the period.
    /// </summary>
    public int TotalIncidents { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="incidents">The incidents.</param>
    /// <param name="incidentsByStatus">The counts by status.</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>A successful result.</returns>
    public static GetComplianceReportResult Success(
        IReadOnlyList<SecurityIncident> incidents,
        IDictionary<SecurityIncidentStatus, int> incidentsByStatus,
        DateTimeOffset startDate,
        DateTimeOffset endDate) => new()
    {
        Succeeded = true,
        Incidents = incidents,
        IncidentsByStatus = incidentsByStatus,
        StartDate = startDate,
        EndDate = endDate,
        TotalIncidents = incidents.Count,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetComplianceReportResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}
