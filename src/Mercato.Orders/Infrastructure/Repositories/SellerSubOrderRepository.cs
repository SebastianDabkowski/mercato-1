using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for seller sub-order data access operations.
/// </summary>
public class SellerSubOrderRepository : ISellerSubOrderRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerSubOrderRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SellerSubOrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SellerSubOrder?> GetByIdAsync(Guid id)
    {
        return await _context.SellerSubOrders
            .Include(s => s.Items)
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerSubOrder>> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.SellerSubOrders
            .Include(s => s.Items)
            .Include(s => s.Order)
            .Where(s => s.StoreId == storeId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerSubOrder>> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.SellerSubOrders
            .Include(s => s.Items)
            .Where(s => s.OrderId == orderId)
            .OrderBy(s => s.StoreName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SellerSubOrder?> GetBySubOrderNumberAsync(string subOrderNumber)
    {
        return await _context.SellerSubOrders
            .Include(s => s.Items)
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.SubOrderNumber == subOrderNumber);
    }

    /// <inheritdoc />
    public async Task<SellerSubOrder> AddAsync(SellerSubOrder sellerSubOrder)
    {
        await _context.SellerSubOrders.AddAsync(sellerSubOrder);
        await _context.SaveChangesAsync();
        return sellerSubOrder;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SellerSubOrder sellerSubOrder)
    {
        _context.SellerSubOrders.Update(sellerSubOrder);
        await _context.SaveChangesAsync();
    }
}
