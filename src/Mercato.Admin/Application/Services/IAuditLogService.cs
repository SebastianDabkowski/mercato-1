using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing and querying admin audit logs.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Gets audit logs with optional filtering.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="adminUserId">Optional admin user ID filter.</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="action">Optional action filter.</param>
    /// <param name="entityId">Optional entity ID filter.</param>
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
}
