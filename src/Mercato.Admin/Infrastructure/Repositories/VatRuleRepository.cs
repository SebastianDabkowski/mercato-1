using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for VAT rules using Entity Framework Core.
/// </summary>
public class VatRuleRepository : IVatRuleRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="VatRuleRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public VatRuleRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<VatRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VatRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<VatRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.VatRules
            .OrderBy(r => r.CountryCode)
            .ThenBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VatRule?> GetActiveByCountryAsync(
        string countryCode,
        Guid? categoryId,
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(countryCode);

        var normalizedCountryCode = countryCode.ToUpperInvariant();

        var query = _dbContext.VatRules
            .Where(r => r.IsActive)
            .Where(r => r.CountryCode == normalizedCountryCode)
            .Where(r => r.EffectiveFrom <= asOfDate)
            .Where(r => r.EffectiveTo == null || r.EffectiveTo > asOfDate);

        if (categoryId.HasValue)
        {
            query = query.Where(r => r.CategoryId == categoryId);
        }
        else
        {
            query = query.Where(r => r.CategoryId == null);
        }

        // Order by priority descending (higher priority first), then by EffectiveFrom descending
        // to get the most recent and highest priority rule
        return await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VatRule> AddAsync(VatRule vatRule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vatRule);
        await _dbContext.VatRules.AddAsync(vatRule, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return vatRule;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(VatRule vatRule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vatRule);
        _dbContext.VatRules.Update(vatRule);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vatRule = await GetByIdAsync(id, cancellationToken);
        if (vatRule != null)
        {
            _dbContext.VatRules.Remove(vatRule);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
