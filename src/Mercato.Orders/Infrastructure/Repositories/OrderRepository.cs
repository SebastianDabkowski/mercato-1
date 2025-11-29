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
        // Build the base query for filtering (without includes for counting)
        var baseQuery = _context.Orders.Where(o => o.BuyerId == buyerId);

        // Apply status filter
        if (statuses != null && statuses.Count > 0)
        {
            baseQuery = baseQuery.Where(o => statuses.Contains(o.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            // Include the entire end date by adding one day and using less than
            var endOfDay = toDate.Value.Date.AddDays(1);
            baseQuery = baseQuery.Where(o => o.CreatedAt < endOfDay);
        }

        // Apply seller (store) filter
        if (storeId.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.SellerSubOrders.Any(s => s.StoreId == storeId.Value));
        }

        // Get total count without includes (more efficient)
        var totalCount = await baseQuery.CountAsync();

        // Apply includes, sorting, and pagination for the data query
        var orders = await baseQuery
            .Include(o => o.Items)
            .Include(o => o.SellerSubOrders)
                .ThenInclude(s => s.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Guid StoreId, string StoreName)>> GetDistinctSellersByBuyerIdAsync(string buyerId)
    {
        return await _context.SellerSubOrders
            .Where(s => s.Order.BuyerId == buyerId)
            .Select(s => new { s.StoreId, s.StoreName })
            .Distinct()
            .OrderBy(s => s.StoreName)
            .Select(s => ValueTuple.Create(s.StoreId, s.StoreName))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetFilteredForAdminAsync(
        IReadOnlyList<OrderStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? searchTerm,
        int page,
        int pageSize)
    {
        // Build the base query for filtering
        var baseQuery = _context.Orders.AsQueryable();

        // Apply status filter
        if (statuses != null && statuses.Count > 0)
        {
            baseQuery = baseQuery.Where(o => statuses.Contains(o.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            baseQuery = baseQuery.Where(o => o.CreatedAt < endOfDay);
        }

        // Apply search term filter (order number, buyer email, or store name)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            baseQuery = baseQuery.Where(o =>
                o.OrderNumber.ToLower().Contains(searchLower) ||
                (o.BuyerEmail != null && o.BuyerEmail.ToLower().Contains(searchLower)) ||
                o.SellerSubOrders.Any(s => s.StoreName.ToLower().Contains(searchLower)));
        }

        // Get total count
        var totalCount = await baseQuery.CountAsync();

        // Apply includes, sorting, and pagination
        var orders = await baseQuery
            .Include(o => o.Items)
            .Include(o => o.SellerSubOrders)
                .ThenInclude(s => s.Items)
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
