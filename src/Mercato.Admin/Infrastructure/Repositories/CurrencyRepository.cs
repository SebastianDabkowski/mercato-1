using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for currencies using Entity Framework Core.
/// </summary>
public class CurrencyRepository : ICurrencyRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public CurrencyRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Currencies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        var normalizedCode = code.ToUpperInvariant();
        return await _dbContext.Currencies
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Currencies
            .OrderByDescending(c => c.IsBaseCurrency)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Currency>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Currencies
            .Where(c => c.IsEnabled)
            .OrderByDescending(c => c.IsBaseCurrency)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Currencies
            .FirstOrDefaultAsync(c => c.IsBaseCurrency, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Currency> AddAsync(Currency currency, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);
        await _dbContext.Currencies.AddAsync(currency, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return currency;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Currency currency, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currency);
        _dbContext.Currencies.Update(currency);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currency = await GetByIdAsync(id, cancellationToken);
        if (currency != null)
        {
            _dbContext.Currencies.Remove(currency);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
