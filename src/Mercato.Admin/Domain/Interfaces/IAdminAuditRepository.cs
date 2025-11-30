using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for admin audit log data access operations.
/// </summary>
public interface IAdminAuditRepository
{
    /// <summary>
    /// Adds a new audit log entry.
    /// </summary>
    /// <param name="auditLog">The audit log entry to add.</param>
    /// <returns>The added audit log entry.</returns>
    Task<AdminAuditLog> AddAsync(AdminAuditLog auditLog);

    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>A list of audit log entries.</returns>
    Task<IReadOnlyList<AdminAuditLog>> GetByEntityAsync(string entityType, string entityId);

    /// <summary>
    /// Gets audit logs by admin user ID.
    /// </summary>
    /// <param name="adminUserId">The admin user ID.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <returns>A list of audit log entries.</returns>
    Task<IReadOnlyList<AdminAuditLog>> GetByAdminUserAsync(string adminUserId, DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null);

    /// <summary>
    /// Gets audit logs with comprehensive filtering options.
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
    Task<IReadOnlyList<AdminAuditLog>> GetFilteredAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        string? adminUserId = null,
        string? entityType = null,
        string? action = null,
        string? entityId = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default);
}
