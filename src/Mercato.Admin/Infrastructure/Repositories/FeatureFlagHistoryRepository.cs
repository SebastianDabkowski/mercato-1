using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for feature flag history using Entity Framework Core.
/// </summary>
public class FeatureFlagHistoryRepository : IFeatureFlagHistoryRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagHistoryRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public FeatureFlagHistoryRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FeatureFlagHistory>> GetByFeatureFlagIdAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FeatureFlagHistories
            .Where(h => h.FeatureFlagId == featureFlagId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<FeatureFlagHistory> AddAsync(FeatureFlagHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        await _dbContext.FeatureFlagHistories.AddAsync(history, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return history;
    }
}
