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
    public async Task<IReadOnlyList<AdminAuditLog>> GetAuditLogsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? adminUserId = null,
        string? entityType = null,
        string? action = null,
        string? entityId = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving audit logs with filters: StartDate={StartDate}, EndDate={EndDate}, AdminUserId={AdminUserId}, EntityType={EntityType}, Action={Action}, EntityId={EntityId}, MaxResults={MaxResults}",
            startDate,
            endDate,
            adminUserId,
            entityType,
            action,
            entityId,
            maxResults);

        return await _repository.GetFilteredAsync(
            startDate,
            endDate,
            adminUserId,
            entityType,
            action,
            entityId,
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
}
