using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for return request data access operations.
/// </summary>
public class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReturnRequestRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ReturnRequestRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ReturnRequest?> GetByIdAsync(Guid id)
    {
        return await _context.ReturnRequests
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Order)
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<ReturnRequest?> GetBySellerSubOrderIdAsync(Guid sellerSubOrderId)
    {
        return await _context.ReturnRequests
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Order)
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(r => r.SellerSubOrderId == sellerSubOrderId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReturnRequest>> GetByBuyerIdAsync(string buyerId)
    {
        return await _context.ReturnRequests
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Order)
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Items)
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReturnRequest>> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.ReturnRequests
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Order)
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Items)
            .Where(r => r.SellerSubOrder.StoreId == storeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ReturnRequest> AddAsync(ReturnRequest returnRequest)
    {
        await _context.ReturnRequests.AddAsync(returnRequest);
        await _context.SaveChangesAsync();
        return returnRequest;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ReturnRequest returnRequest)
    {
        _context.ReturnRequests.Update(returnRequest);
        await _context.SaveChangesAsync();
    }
}
