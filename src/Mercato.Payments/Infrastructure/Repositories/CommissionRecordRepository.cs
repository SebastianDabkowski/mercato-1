using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for commission record data access operations.
/// </summary>
public class CommissionRecordRepository : ICommissionRecordRepository
{
    private readonly PaymentDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionRecordRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The payment database context.</param>
    public CommissionRecordRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<CommissionRecord?> GetByIdAsync(Guid id)
    {
        return await _dbContext.CommissionRecords.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommissionRecord>> GetByPaymentTransactionIdAsync(Guid paymentTransactionId)
    {
        return await _dbContext.CommissionRecords
            .Where(r => r.PaymentTransactionId == paymentTransactionId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommissionRecord>> GetByOrderIdAsync(Guid orderId)
    {
        return await _dbContext.CommissionRecords
            .Where(r => r.OrderId == orderId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommissionRecord>> GetBySellerIdAsync(Guid sellerId)
    {
        return await _dbContext.CommissionRecords
            .Where(r => r.SellerId == sellerId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CommissionRecord?> GetByOrderIdAndSellerIdAsync(Guid orderId, Guid sellerId)
    {
        return await _dbContext.CommissionRecords
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.SellerId == sellerId);
    }

    /// <inheritdoc />
    public async Task<CommissionRecord> AddAsync(CommissionRecord record)
    {
        await _dbContext.CommissionRecords.AddAsync(record);
        await _dbContext.SaveChangesAsync();
        return record;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<CommissionRecord> records)
    {
        await _dbContext.CommissionRecords.AddRangeAsync(records);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CommissionRecord record)
    {
        _dbContext.CommissionRecords.Update(record);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateRangeAsync(IEnumerable<CommissionRecord> records)
    {
        _dbContext.CommissionRecords.UpdateRange(records);
        await _dbContext.SaveChangesAsync();
    }
}
