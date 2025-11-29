using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for settlement data access operations.
/// </summary>
public class SettlementRepository : ISettlementRepository
{
    private readonly PaymentDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettlementRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The payment database context.</param>
    public SettlementRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Settlement?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Settlements.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<Settlement?> GetByIdWithLineItemsAsync(Guid id)
    {
        return await _dbContext.Settlements
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<Settlement?> GetBySellerAndPeriodAsync(Guid sellerId, int year, int month)
    {
        return await _dbContext.Settlements
            .FirstOrDefaultAsync(s => s.SellerId == sellerId && s.Year == year && s.Month == month);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Settlement>> GetBySellerIdAsync(Guid sellerId)
    {
        return await _dbContext.Settlements
            .Where(s => s.SellerId == sellerId)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Settlement>> GetByPeriodAsync(int year, int month)
    {
        return await _dbContext.Settlements
            .Where(s => s.Year == year && s.Month == month)
            .OrderBy(s => s.SellerId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Settlement>> GetFilteredAsync(
        Guid? sellerId,
        int? year,
        int? month,
        SettlementStatus? status)
    {
        var query = _dbContext.Settlements.AsQueryable();

        if (sellerId.HasValue)
        {
            query = query.Where(s => s.SellerId == sellerId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(s => s.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(s => s.Month == month.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.SellerId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Settlement> AddAsync(Settlement settlement)
    {
        await _dbContext.Settlements.AddAsync(settlement);
        await _dbContext.SaveChangesAsync();
        return settlement;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Settlement settlement)
    {
        _dbContext.Settlements.Update(settlement);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task AddLineItemAsync(SettlementLineItem lineItem)
    {
        await _dbContext.SettlementLineItems.AddAsync(lineItem);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteLineItemsAsync(Guid settlementId)
    {
        var lineItems = await _dbContext.SettlementLineItems
            .Where(li => li.SettlementId == settlementId)
            .ToListAsync();

        _dbContext.SettlementLineItems.RemoveRange(lineItems);
        await _dbContext.SaveChangesAsync();
    }
}
