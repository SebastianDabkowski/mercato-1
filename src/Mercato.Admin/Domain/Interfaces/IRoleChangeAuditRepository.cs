using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for managing role change audit logs.
/// </summary>
public interface IRoleChangeAuditRepository
{
    /// <summary>
    /// Adds a new role change audit log entry.
    /// </summary>
    /// <param name="auditLog">The audit log entry to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(RoleChangeAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit logs for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to get audit logs for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audit logs for the specified user.</returns>
    Task<IReadOnlyList<RoleChangeAuditLog>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
