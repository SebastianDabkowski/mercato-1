using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for refund data access operations.
/// </summary>
public class RefundRepository : IRefundRepository
{
    private readonly PaymentDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefundRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The payment database context.</param>
    public RefundRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Refund?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Refunds.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Refund>> GetByPaymentTransactionIdAsync(Guid paymentTransactionId)
    {
        return await _dbContext.Refunds
            .Where(r => r.PaymentTransactionId == paymentTransactionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Refund>> GetByOrderIdAsync(Guid orderId)
    {
        return await _dbContext.Refunds
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Refund>> GetBySellerIdAsync(Guid sellerId)
    {
        return await _dbContext.Refunds
            .Where(r => r.SellerId == sellerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalRefundedByOrderIdAsync(Guid orderId)
    {
        return await _dbContext.Refunds
            .Where(r => r.OrderId == orderId && r.Status == RefundStatus.Completed)
            .SumAsync(r => r.Amount);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalRefundedByOrderIdAndSellerIdAsync(Guid orderId, Guid sellerId)
    {
        return await _dbContext.Refunds
            .Where(r => r.OrderId == orderId && r.SellerId == sellerId && r.Status == RefundStatus.Completed)
            .SumAsync(r => r.Amount);
    }

    /// <inheritdoc />
    public async Task<Refund> AddAsync(Refund refund)
    {
        await _dbContext.Refunds.AddAsync(refund);
        await _dbContext.SaveChangesAsync();
        return refund;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Refund refund)
    {
        _dbContext.Refunds.Update(refund);
        await _dbContext.SaveChangesAsync();
    }
}
