using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for admin audit log data access operations.
/// </summary>
public class AdminAuditRepository : IAdminAuditRepository
{
    private readonly AdminDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminAuditRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AdminAuditRepository(AdminDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<AdminAuditLog> AddAsync(AdminAuditLog auditLog)
    {
        await _context.AdminAuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
        return auditLog;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminAuditLog>> GetByEntityAsync(string entityType, string entityId)
    {
        return await _context.AdminAuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminAuditLog>> GetByAdminUserAsync(string adminUserId, DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null)
    {
        var query = _context.AdminAuditLogs.Where(a => a.AdminUserId == adminUserId);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }
}
