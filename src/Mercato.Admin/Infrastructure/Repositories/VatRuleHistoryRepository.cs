using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for VAT rule history using Entity Framework Core.
/// </summary>
public class VatRuleHistoryRepository : IVatRuleHistoryRepository
{
    private readonly AdminDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="VatRuleHistoryRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public VatRuleHistoryRepository(AdminDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<VatRuleHistory>> GetByVatRuleIdAsync(Guid vatRuleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VatRuleHistories
            .Where(h => h.VatRuleId == vatRuleId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VatRuleHistory> AddAsync(VatRuleHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        await _dbContext.VatRuleHistories.AddAsync(history, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return history;
    }
}
