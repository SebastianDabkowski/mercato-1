using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Domain.Interfaces;
using Mercato.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Payments.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for escrow entry data access operations.
/// </summary>
public class EscrowRepository : IEscrowRepository
{
    private readonly PaymentDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EscrowRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The payment database context.</param>
    public EscrowRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<EscrowEntry?> GetByIdAsync(Guid id)
    {
        return await _dbContext.EscrowEntries.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EscrowEntry>> GetByOrderIdAsync(Guid orderId)
    {
        return await _dbContext.EscrowEntries
            .Where(e => e.OrderId == orderId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EscrowEntry>> GetByPaymentTransactionIdAsync(Guid paymentTransactionId)
    {
        return await _dbContext.EscrowEntries
            .Where(e => e.PaymentTransactionId == paymentTransactionId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EscrowEntry>> GetBySellerIdAsync(Guid sellerId)
    {
        return await _dbContext.EscrowEntries
            .Where(e => e.SellerId == sellerId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EscrowEntry>> GetBySellerIdAndStatusAsync(Guid sellerId, EscrowStatus status)
    {
        return await _dbContext.EscrowEntries
            .Where(e => e.SellerId == sellerId && e.Status == status)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<EscrowEntry> AddAsync(EscrowEntry entry)
    {
        await _dbContext.EscrowEntries.AddAsync(entry);
        await _dbContext.SaveChangesAsync();
        return entry;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<EscrowEntry> entries)
    {
        await _dbContext.EscrowEntries.AddRangeAsync(entries);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(EscrowEntry entry)
    {
        _dbContext.EscrowEntries.Update(entry);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateRangeAsync(IEnumerable<EscrowEntry> entries)
    {
        _dbContext.EscrowEntries.UpdateRange(entries);
        await _dbContext.SaveChangesAsync();
    }
}
