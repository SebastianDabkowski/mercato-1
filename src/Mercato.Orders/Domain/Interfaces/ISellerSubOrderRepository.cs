using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for seller sub-order data access operations.
/// </summary>
public interface ISellerSubOrderRepository
{
    /// <summary>
    /// Gets a seller sub-order by its unique identifier.
    /// </summary>
    /// <param name="id">The seller sub-order ID.</param>
    /// <returns>The seller sub-order if found; otherwise, null.</returns>
    Task<SellerSubOrder?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all seller sub-orders for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of seller sub-orders for the store.</returns>
    Task<IReadOnlyList<SellerSubOrder>> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets all seller sub-orders for a specific parent order.
    /// </summary>
    /// <param name="orderId">The parent order ID.</param>
    /// <returns>A list of seller sub-orders for the parent order.</returns>
    Task<IReadOnlyList<SellerSubOrder>> GetByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Gets a seller sub-order by its sub-order number.
    /// </summary>
    /// <param name="subOrderNumber">The sub-order number.</param>
    /// <returns>The seller sub-order if found; otherwise, null.</returns>
    Task<SellerSubOrder?> GetBySubOrderNumberAsync(string subOrderNumber);

    /// <summary>
    /// Adds a new seller sub-order to the repository.
    /// </summary>
    /// <param name="sellerSubOrder">The seller sub-order to add.</param>
    /// <returns>The added seller sub-order.</returns>
    Task<SellerSubOrder> AddAsync(SellerSubOrder sellerSubOrder);

    /// <summary>
    /// Updates an existing seller sub-order.
    /// </summary>
    /// <param name="sellerSubOrder">The seller sub-order to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(SellerSubOrder sellerSubOrder);

    /// <summary>
    /// Gets a filtered and paginated list of seller sub-orders for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="statuses">Optional list of statuses to filter by.</param>
    /// <param name="fromDate">Optional start date for date range filter (inclusive).</param>
    /// <param name="toDate">Optional end date for date range filter (inclusive).</param>
    /// <param name="buyerSearchTerm">Optional buyer email/name partial match search term.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of sub-orders for the current page and the total count.</returns>
    Task<(IReadOnlyList<SellerSubOrder> SubOrders, int TotalCount)> GetFilteredByStoreIdAsync(
        Guid storeId,
        IReadOnlyList<SellerSubOrderStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? buyerSearchTerm,
        int page,
        int pageSize);

    /// <summary>
    /// Gets distinct buyers from a store's sub-orders.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of distinct buyer IDs and emails.</returns>
    Task<IReadOnlyList<(string BuyerId, string BuyerEmail)>> GetDistinctBuyersByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets a seller sub-order item by its unique identifier, including the parent sub-order and order.
    /// </summary>
    /// <param name="itemId">The seller sub-order item ID.</param>
    /// <returns>The seller sub-order item if found; otherwise, null.</returns>
    Task<SellerSubOrderItem?> GetItemByIdAsync(Guid itemId);
}
