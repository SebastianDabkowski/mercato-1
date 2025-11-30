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

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminAuditLog>> GetFilteredAsync(
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
        var query = _context.AdminAuditLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(adminUserId))
        {
            query = query.Where(a => a.AdminUserId == adminUserId);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrEmpty(entityId))
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        if (isSuccess.HasValue)
        {
            query = query.Where(a => a.IsSuccess == isSuccess.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteOlderThanAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        return await _context.AdminAuditLogs
            .Where(a => a.Timestamp < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminAuditLog>> GetLogsForArchivalAsync(
        DateTimeOffset olderThan,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        return await _context.AdminAuditLogs
            .Where(a => a.Timestamp < olderThan)
            .OrderBy(a => a.Timestamp)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
