using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for role change audit logs.
/// </summary>
public class RoleChangeAuditRepository : IRoleChangeAuditRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleChangeAuditRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public RoleChangeAuditRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task AddAsync(RoleChangeAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        await _dbContext.RoleChangeAuditLogs.AddAsync(auditLog, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RoleChangeAuditLog>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        return await _dbContext.RoleChangeAuditLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.PerformedAt)
            .ToListAsync(cancellationToken);
    }
}
