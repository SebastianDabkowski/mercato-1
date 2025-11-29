using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shipping status history data access.
/// </summary>
public class ShippingStatusHistoryRepository : IShippingStatusHistoryRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingStatusHistoryRepository"/> class.
    /// </summary>
    /// <param name="context">The order database context.</param>
    public ShippingStatusHistoryRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingStatusHistory>> GetBySellerSubOrderIdAsync(Guid sellerSubOrderId)
    {
        return await _context.ShippingStatusHistories
            .Where(h => h.SellerSubOrderId == sellerSubOrderId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ShippingStatusHistory>>> GetBySellerSubOrderIdsAsync(IEnumerable<Guid> sellerSubOrderIds)
    {
        var idList = sellerSubOrderIds.ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<ShippingStatusHistory>>();
        }

        var histories = await _context.ShippingStatusHistories
            .Where(h => idList.Contains(h.SellerSubOrderId))
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        return histories
            .GroupBy(h => h.SellerSubOrderId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ShippingStatusHistory>)g.ToList());
    }

    /// <inheritdoc />
    public async Task<ShippingStatusHistory> AddAsync(ShippingStatusHistory history)
    {
        _context.ShippingStatusHistories.Add(history);
        await _context.SaveChangesAsync();
        return history;
    }
}
