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

    /// <inheritdoc />
    public async Task<(IReadOnlyList<SellerSubOrder> SubOrders, int TotalCount)> GetFilteredByStoreIdAsync(
        Guid storeId,
        IReadOnlyList<SellerSubOrderStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? buyerSearchTerm,
        int page,
        int pageSize)
    {
        // Build the base query for filtering (without includes for counting)
        var baseQuery = _context.SellerSubOrders.Where(s => s.StoreId == storeId);

        // Apply status filter
        if (statuses != null && statuses.Count > 0)
        {
            baseQuery = baseQuery.Where(s => statuses.Contains(s.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            // Include the entire end date by adding one day and using less than
            var endOfDay = toDate.Value.Date.AddDays(1);
            baseQuery = baseQuery.Where(s => s.CreatedAt < endOfDay);
        }

        // Apply buyer search filter (search by BuyerId from parent Order)
        // Using Contains with StringComparison for case-insensitive search that EF Core can translate
        if (!string.IsNullOrWhiteSpace(buyerSearchTerm))
        {
            baseQuery = baseQuery.Where(s => s.Order != null && 
                EF.Functions.Like(s.Order.BuyerId, $"%{buyerSearchTerm}%"));
        }

        // Get total count without includes (more efficient)
        var totalCount = await baseQuery.CountAsync();

        // Apply includes, sorting, and pagination for the data query
        var subOrders = await baseQuery
            .Include(s => s.Items)
            .Include(s => s.Order)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (subOrders, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(string BuyerId, string BuyerEmail)>> GetDistinctBuyersByStoreIdAsync(Guid storeId)
    {
        // Note: BuyerEmail is not stored directly in Orders module; using BuyerId as placeholder.
        // The buyer email would need to be fetched from the Identity module if needed for display.
        return await _context.SellerSubOrders
            .Where(s => s.StoreId == storeId && s.Order != null)
            .Select(s => new { s.Order.BuyerId })
            .Distinct()
            .OrderBy(s => s.BuyerId)
            .Select(s => ValueTuple.Create(s.BuyerId, s.BuyerId))
            .ToListAsync();
    }
}
