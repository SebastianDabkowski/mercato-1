using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for order data access operations.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.SellerSubOrders)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <inheritdoc />
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.SellerSubOrders)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Order>> GetByBuyerIdAsync(string buyerId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.SellerSubOrders)
                .ThenInclude(s => s.Items)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Order?> GetByPaymentTransactionIdAsync(Guid transactionId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.SellerSubOrders)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(o => o.PaymentTransactionId == transactionId);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetFilteredByBuyerIdAsync(
        string buyerId,
        IReadOnlyList<OrderStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        Guid? storeId,
        int page,
        int pageSize)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .Include(o => o.SellerSubOrders)
                .ThenInclude(s => s.Items)
            .Where(o => o.BuyerId == buyerId);

        // Apply status filter
        if (statuses != null && statuses.Count > 0)
        {
            query = query.Where(o => statuses.Contains(o.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            // Include the entire end date by adding one day and using less than
            var endOfDay = toDate.Value.Date.AddDays(1);
            query = query.Where(o => o.CreatedAt < endOfDay);
        }

        // Apply seller (store) filter
        if (storeId.HasValue)
        {
            query = query.Where(o => o.SellerSubOrders.Any(s => s.StoreId == storeId.Value));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting and pagination
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }

    /// <inheritdoc />
    public async Task<Order> AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        return order;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }
}
