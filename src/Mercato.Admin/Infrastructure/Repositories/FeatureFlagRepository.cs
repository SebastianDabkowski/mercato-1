using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for feature flags using Entity Framework Core.
/// </summary>
public class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public FeatureFlagRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<FeatureFlag?> GetByKeyAndEnvironmentAsync(string key, FeatureFlagEnvironment environment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        return await _dbContext.FeatureFlags
            .FirstOrDefaultAsync(f => f.Key == key && f.Environment == environment, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.FeatureFlags
            .OrderBy(f => f.Environment)
            .ThenBy(f => f.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FeatureFlag>> GetByEnvironmentAsync(FeatureFlagEnvironment environment, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FeatureFlags
            .Where(f => f.Environment == environment)
            .OrderBy(f => f.Key)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<FeatureFlag> AddAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(featureFlag);
        await _dbContext.FeatureFlags.AddAsync(featureFlag, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return featureFlag;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(featureFlag);
        _dbContext.FeatureFlags.Update(featureFlag);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var featureFlag = await GetByIdAsync(id, cancellationToken);
        if (featureFlag != null)
        {
            _dbContext.FeatureFlags.Remove(featureFlag);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
