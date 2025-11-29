using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for payout data access operations.
/// </summary>
public class PayoutRepository : IPayoutRepository
{
    private readonly PaymentDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PayoutRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The payment database context.</param>
    public PayoutRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Payout?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Payouts.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payout>> GetBySellerIdAsync(Guid sellerId)
    {
        return await _dbContext.Payouts
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.ScheduledAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payout>> GetBySellerIdAndStatusAsync(Guid sellerId, PayoutStatus status)
    {
        return await _dbContext.Payouts
            .Where(p => p.SellerId == sellerId && p.Status == status)
            .OrderByDescending(p => p.ScheduledAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payout>> GetByStatusAsync(PayoutStatus status)
    {
        return await _dbContext.Payouts
            .Where(p => p.Status == status)
            .OrderBy(p => p.ScheduledAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payout>> GetByBatchIdAsync(Guid batchId)
    {
        return await _dbContext.Payouts
            .Where(p => p.BatchId == batchId)
            .OrderBy(p => p.SellerId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payout>> GetScheduledPayoutsAsync(DateTimeOffset scheduledBefore)
    {
        return await _dbContext.Payouts
            .Where(p => p.Status == PayoutStatus.Scheduled && p.ScheduledAt <= scheduledBefore)
            .OrderBy(p => p.ScheduledAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payout>> GetPayoutsForRetryAsync(int maxRetryCount)
    {
        return await _dbContext.Payouts
            .Where(p => p.Status == PayoutStatus.Failed && p.RetryCount < maxRetryCount)
            .OrderBy(p => p.CompletedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Payout> AddAsync(Payout payout)
    {
        await _dbContext.Payouts.AddAsync(payout);
        await _dbContext.SaveChangesAsync();
        return payout;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<Payout> payouts)
    {
        await _dbContext.Payouts.AddRangeAsync(payouts);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Payout payout)
    {
        _dbContext.Payouts.Update(payout);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateRangeAsync(IEnumerable<Payout> payouts)
    {
        _dbContext.Payouts.UpdateRange(payouts);
        await _dbContext.SaveChangesAsync();
    }
}
