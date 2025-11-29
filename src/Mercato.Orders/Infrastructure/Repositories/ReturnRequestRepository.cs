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
            .Include(r => r.CaseItems)
                .ThenInclude(ci => ci.SellerSubOrderItem)
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
            .Include(r => r.CaseItems)
                .ThenInclude(ci => ci.SellerSubOrderItem)
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
            .Include(r => r.CaseItems)
                .ThenInclude(ci => ci.SellerSubOrderItem)
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
            .Include(r => r.CaseItems)
                .ThenInclude(ci => ci.SellerSubOrderItem)
            .Where(r => r.SellerSubOrder.StoreId == storeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ReturnRequest> ReturnRequests, int TotalCount)> GetFilteredByStoreIdAsync(
        Guid storeId,
        IReadOnlyList<ReturnStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize)
    {
        var query = _context.ReturnRequests
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Order)
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Items)
            .Include(r => r.CaseItems)
                .ThenInclude(ci => ci.SellerSubOrderItem)
            .Where(r => r.SellerSubOrder.StoreId == storeId);

        // Apply status filter
        if (statuses != null && statuses.Count > 0)
        {
            query = query.Where(r => statuses.Contains(r.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            // Add one day to include the entire "to" day, preserving timezone as UTC
            var endDate = new DateTimeOffset(toDate.Value.Date.AddDays(1), TimeSpan.Zero);
            query = query.Where(r => r.CreatedAt < endDate);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply ordering and pagination
        var returnRequests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (returnRequests, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReturnRequest>> GetOpenCasesForItemsAsync(IEnumerable<Guid> itemIds)
    {
        var itemIdList = itemIds.ToList();
        if (itemIdList.Count == 0)
        {
            return [];
        }

        // Open cases are those not in Completed or Rejected status
        return await _context.ReturnRequests
            .Include(r => r.CaseItems)
            .Where(r => r.Status != ReturnStatus.Completed && r.Status != ReturnStatus.Rejected)
            .Where(r => r.CaseItems.Any(ci => itemIdList.Contains(ci.SellerSubOrderItemId)))
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
