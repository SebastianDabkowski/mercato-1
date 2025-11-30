using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for security incident management operations.
/// </summary>
public class SecurityIncidentService : ISecurityIncidentService
{
    private readonly ISecurityIncidentRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SecurityIncidentService> _logger;

    /// <summary>
    /// The severity threshold at or above which alerts are sent.
    /// </summary>
    private const SecurityIncidentSeverity AlertThreshold = SecurityIncidentSeverity.High;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityIncidentService"/> class.
    /// </summary>
    /// <param name="repository">The security incident repository.</param>
    /// <param name="notificationService">The notification service for sending alerts.</param>
    /// <param name="logger">The logger.</param>
    public SecurityIncidentService(
        ISecurityIncidentRepository repository,
        INotificationService notificationService,
        ILogger<SecurityIncidentService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CreateSecurityIncidentResult> CreateIncidentAsync(
        string source,
        string detectionRule,
        SecurityIncidentSeverity severity,
        string description,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateCreateIncident(source, detectionRule, description);
        if (validationErrors.Count > 0)
        {
            return CreateSecurityIncidentResult.Failure(validationErrors);
        }

        var now = DateTimeOffset.UtcNow;
        var incident = new SecurityIncident
        {
            Id = Guid.NewGuid(),
            DetectedAt = now,
            Source = source,
            DetectionRule = detectionRule,
            Severity = severity,
            Status = SecurityIncidentStatus.Open,
            Description = description,
            Details = details,
            CreatedAt = now,
            UpdatedAt = now,
            AlertsSent = false
        };

        _logger.LogInformation(
            "Creating security incident: DetectionRule={DetectionRule}, Severity={Severity}, Source={Source}",
            detectionRule,
            severity,
            source);

        var createdIncident = await _repository.AddAsync(incident, cancellationToken);

        // Send alerts for high-severity incidents
        var alertsTriggered = false;
        if (severity >= AlertThreshold)
        {
            alertsTriggered = await SendHighSeverityAlertAsync(createdIncident, cancellationToken);
            if (alertsTriggered)
            {
                createdIncident.AlertsSent = true;
                await _repository.UpdateAsync(createdIncident, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Security incident created: Id={IncidentId}, AlertsTriggered={AlertsTriggered}",
            createdIncident.Id,
            alertsTriggered);

        return CreateSecurityIncidentResult.Success(createdIncident, alertsTriggered);
    }

    /// <inheritdoc/>
    public async Task<UpdateSecurityIncidentStatusResult> UpdateStatusAsync(
        Guid incidentId,
        SecurityIncidentStatus newStatus,
        string userId,
        string? notes = null,
        string? resolutionNotes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UpdateSecurityIncidentStatusResult.Failure("User ID is required.");
        }

        var incident = await _repository.GetByIdAsync(incidentId, cancellationToken);
        if (incident == null)
        {
            return UpdateSecurityIncidentStatusResult.Failure("Incident not found.");
        }

        var previousStatus = incident.Status;
        if (previousStatus == newStatus)
        {
            return UpdateSecurityIncidentStatusResult.Failure("Status is already set to the requested value.");
        }

        _logger.LogInformation(
            "Updating security incident status: Id={IncidentId}, PreviousStatus={PreviousStatus}, NewStatus={NewStatus}, UserId={UserId}",
            incidentId,
            previousStatus,
            newStatus,
            userId);

        var now = DateTimeOffset.UtcNow;

        // Record the status change
        var statusChange = new SecurityIncidentStatusChange
        {
            Id = Guid.NewGuid(),
            SecurityIncidentId = incidentId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = userId,
            ChangedAt = now,
            Notes = notes
        };

        await _repository.AddStatusChangeAsync(statusChange, cancellationToken);

        // Update the incident
        incident.Status = newStatus;
        incident.UpdatedAt = now;

        // Handle resolution
        if (newStatus == SecurityIncidentStatus.Resolved || newStatus == SecurityIncidentStatus.FalsePositive)
        {
            incident.ResolvedAt = now;
            incident.ResolvedByUserId = userId;
            if (!string.IsNullOrEmpty(resolutionNotes))
            {
                incident.ResolutionNotes = resolutionNotes;
            }
        }

        var updatedIncident = await _repository.UpdateAsync(incident, cancellationToken);

        _logger.LogInformation(
            "Security incident status updated: Id={IncidentId}, NewStatus={NewStatus}",
            incidentId,
            newStatus);

        return UpdateSecurityIncidentStatusResult.Success(updatedIncident);
    }

    /// <inheritdoc/>
    public async Task<GetSecurityIncidentResult> GetIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await _repository.GetByIdAsync(incidentId, cancellationToken);
        if (incident == null)
        {
            return GetSecurityIncidentResult.Failure("Incident not found.");
        }

        return GetSecurityIncidentResult.Success(incident);
    }

    /// <inheritdoc/>
    public async Task<GetSecurityIncidentsResult> GetIncidentsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        SecurityIncidentSeverity? severity = null,
        SecurityIncidentStatus? status = null,
        string? detectionRule = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving security incidents with filters: StartDate={StartDate}, EndDate={EndDate}, Severity={Severity}, Status={Status}, DetectionRule={DetectionRule}",
            startDate,
            endDate,
            severity,
            status,
            detectionRule);

        var incidents = await _repository.GetFilteredAsync(
            startDate,
            endDate,
            severity,
            status,
            detectionRule,
            maxResults,
            cancellationToken);

        return GetSecurityIncidentsResult.Success(incidents);
    }

    /// <inheritdoc/>
    public async Task<GetStatusChangesResult> GetStatusHistoryAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await _repository.GetByIdAsync(incidentId, cancellationToken);
        if (incident == null)
        {
            return GetStatusChangesResult.Failure("Incident not found.");
        }

        var statusChanges = await _repository.GetStatusChangesAsync(incidentId, cancellationToken);
        return GetStatusChangesResult.Success(statusChanges);
    }

    /// <inheritdoc/>
    public async Task<GetComplianceReportResult> GetComplianceReportAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            return GetComplianceReportResult.Failure("End date must be after start date.");
        }

        _logger.LogInformation(
            "Generating compliance report: StartDate={StartDate}, EndDate={EndDate}",
            startDate,
            endDate);

        var incidents = await _repository.GetFilteredAsync(
            startDate,
            endDate,
            maxResults: 10000,
            cancellationToken: cancellationToken);

        var incidentsByStatus = await _repository.GetIncidentCountsByStatusAsync(
            startDate,
            endDate,
            cancellationToken);

        return GetComplianceReportResult.Success(incidents, incidentsByStatus, startDate, endDate);
    }

    /// <summary>
    /// Validates the create incident input.
    /// </summary>
    private static List<string> ValidateCreateIncident(string source, string detectionRule, string description)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(source))
        {
            errors.Add("Source is required.");
        }

        if (string.IsNullOrWhiteSpace(detectionRule))
        {
            errors.Add("Detection rule is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Description is required.");
        }

        return errors;
    }

    /// <summary>
    /// Sends an alert notification for high-severity incidents.
    /// </summary>
    private async Task<bool> SendHighSeverityAlertAsync(SecurityIncident incident, CancellationToken cancellationToken)
    {
        try
        {
            // Create a notification for security contacts
            // In a real implementation, this would send to configured security contacts via email or integrations
            // For now, we create an in-app notification with a reserved "security-alerts" user ID
            // The actual email/integration sending would be handled by the notification service or a separate alerting system
            var command = new CreateNotificationCommand
            {
                UserId = "security-alerts",
                Title = $"[{incident.Severity}] Security Incident Detected",
                Message = $"Detection Rule: {incident.DetectionRule}\nDescription: {incident.Description}\nSource: {incident.Source}",
                Type = NotificationType.SecurityIncident,
                RelatedEntityId = incident.Id
            };

            var result = await _notificationService.CreateNotificationAsync(command);

            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Failed to send high-severity alert for incident {IncidentId}: {Errors}",
                    incident.Id,
                    string.Join(", ", result.Errors));
                return false;
            }

            _logger.LogInformation(
                "High-severity alert sent for incident {IncidentId}",
                incident.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending high-severity alert for incident {IncidentId}",
                incident.Id);
            return false;
        }
    }
}
