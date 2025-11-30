using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing and querying admin audit logs.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs a critical action performed in the system.
    /// </summary>
    /// <param name="userId">The ID of the user who performed the action.</param>
    /// <param name="action">The type of action performed.</param>
    /// <param name="entityType">The type of the target resource.</param>
    /// <param name="entityId">The ID of the target resource.</param>
    /// <param name="isSuccess">Whether the action was successful.</param>
    /// <param name="details">Optional additional details about the action.</param>
    /// <param name="failureReason">Optional failure reason if the action was not successful.</param>
    /// <param name="ipAddress">Optional IP address from which the action was performed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created audit log entry.</returns>
    Task<AdminAuditLog> LogCriticalActionAsync(
        string userId,
        string action,
        string entityType,
        string entityId,
        bool isSuccess,
        string? details = null,
        string? failureReason = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs with optional filtering.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="adminUserId">Optional admin user ID filter.</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="action">Optional action filter.</param>
    /// <param name="entityId">Optional entity ID filter.</param>
    /// <param name="isSuccess">Optional success/failure filter.</param>
    /// <param name="maxResults">Maximum number of results to return. Default is 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A filtered list of audit log entries.</returns>
    Task<IReadOnlyList<AdminAuditLog>> GetAuditLogsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? adminUserId = null,
        string? entityType = null,
        string? action = null,
        string? entityId = null,
        bool? isSuccess = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full change history for a specific resource.
    /// </summary>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audit log entries for the specified resource.</returns>
    Task<IReadOnlyList<AdminAuditLog>> GetAuditLogsByResourceAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes audit logs older than the specified retention period.
    /// </summary>
    /// <param name="retentionDays">The number of days to retain logs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of deleted records.</returns>
    Task<int> PurgeOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for archival before deletion.
    /// </summary>
    /// <param name="retentionDays">The number of days to retain logs.</param>
    /// <param name="batchSize">Maximum number of records to retrieve per batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audit log entries ready for archival.</returns>
    Task<IReadOnlyList<AdminAuditLog>> GetLogsForArchivalAsync(
        int retentionDays,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);
}
