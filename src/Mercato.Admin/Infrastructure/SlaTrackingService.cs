using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for SLA tracking operations.
/// </summary>
public class SlaTrackingService : ISlaTrackingService
{
    private readonly ISlaTrackingRepository _trackingRepository;
    private readonly ISlaConfigurationRepository _configurationRepository;
    private readonly ILogger<SlaTrackingService> _logger;

    // Default SLA thresholds if no configuration is found
    private const int DefaultFirstResponseHours = 24;
    private const int DefaultResolutionHours = 72;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlaTrackingService"/> class.
    /// </summary>
    /// <param name="trackingRepository">The SLA tracking repository.</param>
    /// <param name="configurationRepository">The SLA configuration repository.</param>
    /// <param name="logger">The logger.</param>
    public SlaTrackingService(
        ISlaTrackingRepository trackingRepository,
        ISlaConfigurationRepository configurationRepository,
        ILogger<SlaTrackingService> logger)
    {
        _trackingRepository = trackingRepository ?? throw new ArgumentNullException(nameof(trackingRepository));
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<SlaTrackingRecord> CreateTrackingRecordAsync(
        Guid caseId,
        string caseNumber,
        string caseType,
        Guid storeId,
        string storeName,
        DateTimeOffset caseCreatedAt,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        // Get applicable SLA configuration
        var config = await _configurationRepository.GetApplicableConfigurationAsync(caseType, category, cancellationToken);

        var firstResponseHours = config?.FirstResponseDeadlineHours ?? DefaultFirstResponseHours;
        var resolutionHours = config?.ResolutionDeadlineHours ?? DefaultResolutionHours;

        var record = new SlaTrackingRecord
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            CaseNumber = caseNumber,
            CaseType = caseType,
            StoreId = storeId,
            StoreName = storeName,
            CaseCreatedAt = caseCreatedAt,
            FirstResponseDeadline = caseCreatedAt.AddHours(firstResponseHours),
            ResolutionDeadline = caseCreatedAt.AddHours(resolutionHours),
            Status = SlaStatus.Pending,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        await _trackingRepository.AddAsync(record, cancellationToken);

        _logger.LogInformation(
            "Created SLA tracking record for case {CaseNumber} with first response deadline {FirstResponseDeadline} and resolution deadline {ResolutionDeadline}",
            caseNumber,
            record.FirstResponseDeadline,
            record.ResolutionDeadline);

        return record;
    }

    /// <inheritdoc/>
    public async Task RecordFirstResponseAsync(
        Guid caseId,
        DateTimeOffset respondedAt,
        CancellationToken cancellationToken = default)
    {
        var record = await _trackingRepository.GetByCaseIdAsync(caseId, cancellationToken);
        if (record == null)
        {
            _logger.LogWarning("No SLA tracking record found for case {CaseId}", caseId);
            return;
        }

        if (record.FirstResponseAt.HasValue)
        {
            _logger.LogDebug("First response already recorded for case {CaseId}", caseId);
            return;
        }

        record.FirstResponseAt = respondedAt;
        record.IsFirstResponseBreached = respondedAt > record.FirstResponseDeadline;
        record.Status = record.IsFirstResponseBreached ? SlaStatus.FirstResponseBreached : SlaStatus.Responded;
        record.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _trackingRepository.UpdateAsync(record, cancellationToken);

        _logger.LogInformation(
            "Recorded first response for case {CaseNumber} at {ResponseTime}. Breached: {IsBreached}",
            record.CaseNumber,
            respondedAt,
            record.IsFirstResponseBreached);
    }

    /// <inheritdoc/>
    public async Task RecordResolutionAsync(
        Guid caseId,
        DateTimeOffset resolvedAt,
        CancellationToken cancellationToken = default)
    {
        var record = await _trackingRepository.GetByCaseIdAsync(caseId, cancellationToken);
        if (record == null)
        {
            _logger.LogWarning("No SLA tracking record found for case {CaseId}", caseId);
            return;
        }

        record.ResolvedAt = resolvedAt;
        record.IsResolutionBreached = resolvedAt > record.ResolutionDeadline;
        record.Status = record.IsResolutionBreached || record.IsFirstResponseBreached
            ? SlaStatus.Closed
            : SlaStatus.ResolvedWithinSla;
        record.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _trackingRepository.UpdateAsync(record, cancellationToken);

        _logger.LogInformation(
            "Recorded resolution for case {CaseNumber} at {ResolvedAt}. Resolution breached: {IsBreached}",
            record.CaseNumber,
            resolvedAt,
            record.IsResolutionBreached);
    }

    /// <inheritdoc/>
    public async Task<int> CheckAndUpdateBreachesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var pendingRecords = await _trackingRepository.GetByDateRangeAsync(
            DateTimeOffset.MinValue,
            now,
            status: SlaStatus.Pending,
            cancellationToken: cancellationToken);

        var respondedRecords = await _trackingRepository.GetByDateRangeAsync(
            DateTimeOffset.MinValue,
            now,
            status: SlaStatus.Responded,
            cancellationToken: cancellationToken);

        var allToCheck = pendingRecords.Concat(respondedRecords).ToList();
        var breachCount = 0;

        foreach (var record in allToCheck)
        {
            var updated = false;

            // Check first response breach
            if (!record.FirstResponseAt.HasValue && now > record.FirstResponseDeadline && !record.IsFirstResponseBreached)
            {
                record.IsFirstResponseBreached = true;
                record.Status = SlaStatus.FirstResponseBreached;
                updated = true;
            }

            // Check resolution breach
            if (!record.ResolvedAt.HasValue && now > record.ResolutionDeadline && !record.IsResolutionBreached)
            {
                record.IsResolutionBreached = true;
                record.Status = SlaStatus.ResolutionBreached;
                updated = true;
            }

            if (updated)
            {
                record.LastUpdatedAt = now;
                await _trackingRepository.UpdateAsync(record, cancellationToken);
                breachCount++;

                _logger.LogWarning(
                    "SLA breach detected for case {CaseNumber}. First response breached: {FirstBreach}, Resolution breached: {ResolutionBreach}",
                    record.CaseNumber,
                    record.IsFirstResponseBreached,
                    record.IsResolutionBreached);
            }
        }

        return breachCount;
    }

    /// <inheritdoc/>
    public async Task<SlaDashboardStatistics> GetDashboardStatisticsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var records = await _trackingRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken: cancellationToken);
        var breachedCases = await _trackingRepository.GetBreachedCasesAsync(cancellationToken);

        var responseTimes = records
            .Where(r => r.ResponseTimeHours.HasValue)
            .Select(r => r.ResponseTimeHours!.Value)
            .ToList();

        var resolutionTimes = records
            .Where(r => r.ResolutionTimeHours.HasValue)
            .Select(r => r.ResolutionTimeHours!.Value)
            .ToList();

        return new SlaDashboardStatistics
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalCases = records.Count,
            OpenCases = records.Count(r => r.Status == SlaStatus.Pending || r.Status == SlaStatus.Responded),
            CasesResolvedWithinSla = records.Count(r => r.Status == SlaStatus.ResolvedWithinSla),
            CasesRespondedWithinSla = records.Count(r => r.FirstResponseAt.HasValue && !r.IsFirstResponseBreached),
            CurrentlyBreachedCases = breachedCases.Count,
            TotalFirstResponseBreaches = records.Count(r => r.IsFirstResponseBreached),
            TotalResolutionBreaches = records.Count(r => r.IsResolutionBreached),
            AverageResponseTimeHours = responseTimes.Count > 0 ? Math.Round(responseTimes.Average(), 2) : 0,
            AverageResolutionTimeHours = resolutionTimes.Count > 0 ? Math.Round(resolutionTimes.Average(), 2) : 0
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlaStoreStatistics>> GetSellerStatisticsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        return await _trackingRepository.GetAllStoreStatisticsAsync(startDate, endDate, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlaTrackingRecord>> GetBreachedCasesAsync(CancellationToken cancellationToken = default)
    {
        return await _trackingRepository.GetBreachedCasesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlaConfiguration>> GetSlaConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _configurationRepository.GetAllActiveAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SlaConfiguration> SaveSlaConfigurationAsync(
        SlaConfiguration configuration,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.Id == Guid.Empty)
        {
            // New configuration
            configuration.Id = Guid.NewGuid();
            configuration.CreatedAt = DateTimeOffset.UtcNow;
            configuration.CreatedByUserId = adminUserId;
            await _configurationRepository.AddAsync(configuration, cancellationToken);

            _logger.LogInformation(
                "Created new SLA configuration '{Name}' by admin {AdminUserId}",
                configuration.Name,
                adminUserId);
        }
        else
        {
            // Update existing configuration
            configuration.UpdatedAt = DateTimeOffset.UtcNow;
            configuration.UpdatedByUserId = adminUserId;
            await _configurationRepository.UpdateAsync(configuration, cancellationToken);

            _logger.LogInformation(
                "Updated SLA configuration '{Name}' by admin {AdminUserId}",
                configuration.Name,
                adminUserId);
        }

        return configuration;
    }

    /// <inheritdoc/>
    public async Task DeleteSlaConfigurationAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        await _configurationRepository.DeleteAsync(configurationId, cancellationToken);

        _logger.LogInformation("Deleted SLA configuration {ConfigurationId}", configurationId);
    }
}
