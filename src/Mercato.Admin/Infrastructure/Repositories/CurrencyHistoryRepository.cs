using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for currency history using Entity Framework Core.
/// </summary>
public class CurrencyHistoryRepository : ICurrencyHistoryRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyHistoryRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public CurrencyHistoryRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<CurrencyHistory> AddAsync(CurrencyHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        await _dbContext.CurrencyHistories.AddAsync(history, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return history;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CurrencyHistory>> GetByCurrencyIdAsync(Guid currencyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CurrencyHistories
            .Where(h => h.CurrencyId == currencyId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }
}
