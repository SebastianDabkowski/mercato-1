using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for integrations using Entity Framework Core.
/// </summary>
public class IntegrationRepository : IIntegrationRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public IntegrationRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Integrations
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Integration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Integrations
            .OrderBy(i => i.IntegrationType)
            .ThenBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Integration>> GetByTypeAsync(IntegrationType type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Integrations
            .Where(i => i.IntegrationType == type)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Integration>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Integrations
            .Where(i => i.IsEnabled)
            .OrderBy(i => i.IntegrationType)
            .ThenBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Integration> AddAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integration);
        await _dbContext.Integrations.AddAsync(integration, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return integration;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integration);
        _dbContext.Integrations.Update(integration);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var integration = await GetByIdAsync(id, cancellationToken);
        if (integration != null)
        {
            _dbContext.Integrations.Remove(integration);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
