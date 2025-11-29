using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SLA configuration using Entity Framework Core.
/// </summary>
public class SlaConfigurationRepository : ISlaConfigurationRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlaConfigurationRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public SlaConfigurationRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<SlaConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SlaConfigurations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SlaConfiguration>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SlaConfigurations
            .Where(c => c.IsActive)
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SlaConfiguration?> GetApplicableConfigurationAsync(
        string? caseType,
        string? category,
        CancellationToken cancellationToken = default)
    {
        // Find the highest priority (lowest number) configuration that matches
        // Priority order: exact match on both > match on caseType only > match on category only > default (null, null)
        var configurations = await _dbContext.SlaConfigurations
            .Where(c => c.IsActive)
            .OrderBy(c => c.Priority)
            .ToListAsync(cancellationToken);

        // Try exact match first
        var exactMatch = configurations.FirstOrDefault(c =>
            c.CaseType == caseType && c.Category == category);
        if (exactMatch != null) return exactMatch;

        // Try case type match
        var caseTypeMatch = configurations.FirstOrDefault(c =>
            c.CaseType == caseType && c.Category == null);
        if (caseTypeMatch != null) return caseTypeMatch;

        // Try category match
        var categoryMatch = configurations.FirstOrDefault(c =>
            c.CaseType == null && c.Category == category);
        if (categoryMatch != null) return categoryMatch;

        // Fall back to default (null, null)
        return configurations.FirstOrDefault(c =>
            c.CaseType == null && c.Category == null);
    }

    /// <inheritdoc/>
    public async Task<SlaConfiguration> AddAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        await _dbContext.SlaConfigurations.AddAsync(configuration, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return configuration;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _dbContext.SlaConfigurations.Update(configuration);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var configuration = await GetByIdAsync(id, cancellationToken);
        if (configuration != null)
        {
            _dbContext.SlaConfigurations.Remove(configuration);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
