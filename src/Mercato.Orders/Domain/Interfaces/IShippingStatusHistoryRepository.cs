using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for shipping status history data access operations.
/// </summary>
public interface IShippingStatusHistoryRepository
{
    /// <summary>
    /// Gets all shipping status history records for a seller sub-order.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order ID.</param>
    /// <returns>A list of shipping status history records ordered by timestamp.</returns>
    Task<IReadOnlyList<ShippingStatusHistory>> GetBySellerSubOrderIdAsync(Guid sellerSubOrderId);

    /// <summary>
    /// Gets all shipping status history records for multiple seller sub-orders.
    /// </summary>
    /// <param name="sellerSubOrderIds">The seller sub-order IDs.</param>
    /// <returns>A dictionary of shipping status history records keyed by seller sub-order ID.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ShippingStatusHistory>>> GetBySellerSubOrderIdsAsync(IEnumerable<Guid> sellerSubOrderIds);

    /// <summary>
    /// Adds a new shipping status history record.
    /// </summary>
    /// <param name="history">The history record to add.</param>
    /// <returns>The added history record.</returns>
    Task<ShippingStatusHistory> AddAsync(ShippingStatusHistory history);
}
