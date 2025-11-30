using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing and querying admin audit logs.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IAdminAuditRepository _repository;
    private readonly ILogger<AuditLogService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogService"/> class.
    /// </summary>
    /// <param name="repository">The admin audit repository.</param>
    /// <param name="logger">The logger.</param>
    public AuditLogService(
        IAdminAuditRepository repository,
        ILogger<AuditLogService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AdminAuditLog> LogCriticalActionAsync(
        string userId,
        string action,
        string entityType,
        string entityId,
        bool isSuccess,
        string? details = null,
        string? failureReason = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IsSuccess = isSuccess,
            Details = details,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.UtcNow
        };

        _logger.LogInformation(
            "Logging critical action: UserId={UserId}, Action={Action}, EntityType={EntityType}, EntityId={EntityId}, IsSuccess={IsSuccess}",
            userId,
            action,
            entityType,
            entityId,
            isSuccess);

        return await _repository.AddAsync(auditLog);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AdminAuditLog>> GetAuditLogsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? adminUserId = null,
        string? entityType = null,
        string? action = null,
        string? entityId = null,
        bool? isSuccess = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving audit logs with filters: StartDate={StartDate}, EndDate={EndDate}, AdminUserId={AdminUserId}, EntityType={EntityType}, Action={Action}, EntityId={EntityId}, IsSuccess={IsSuccess}, MaxResults={MaxResults}",
            startDate,
            endDate,
            adminUserId,
            entityType,
            action,
            entityId,
            isSuccess,
            maxResults);

        return await _repository.GetFilteredAsync(
            startDate,
            endDate,
            adminUserId,
            entityType,
            action,
            entityId,
            isSuccess,
            maxResults,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AdminAuditLog>> GetAuditLogsByResourceAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving audit logs for resource: EntityType={EntityType}, EntityId={EntityId}",
            entityType,
            entityId);

        return await _repository.GetByEntityAsync(entityType, entityId);
    }

    /// <inheritdoc/>
    public async Task<int> PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        _logger.LogInformation(
            "Purging audit logs older than {CutoffDate} (retention: {RetentionDays} days)",
            cutoffDate,
            retentionDays);

        var deletedCount = await _repository.DeleteOlderThanAsync(cutoffDate, cancellationToken);

        _logger.LogInformation(
            "Purged {DeletedCount} audit log entries older than {CutoffDate}",
            deletedCount,
            cutoffDate);

        return deletedCount;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AdminAuditLog>> GetLogsForArchivalAsync(
        int retentionDays,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        _logger.LogInformation(
            "Retrieving audit logs for archival older than {CutoffDate} (batch size: {BatchSize})",
            cutoffDate,
            batchSize);

        return await _repository.GetLogsForArchivalAsync(cutoffDate, batchSize, cancellationToken);
    }
}
