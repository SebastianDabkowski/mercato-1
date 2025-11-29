using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for order data access operations.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Gets an order by its unique identifier.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <returns>The order if found; otherwise, null.</returns>
    Task<Order?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets an order by its order number.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <returns>The order if found; otherwise, null.</returns>
    Task<Order?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>
    /// Gets all orders for a specific buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A list of orders for the buyer.</returns>
    Task<IReadOnlyList<Order>> GetByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Gets an order by payment transaction ID.
    /// </summary>
    /// <param name="transactionId">The payment transaction ID.</param>
    /// <returns>The order if found; otherwise, null.</returns>
    Task<Order?> GetByPaymentTransactionIdAsync(Guid transactionId);

    /// <summary>
    /// Gets a filtered and paginated list of orders for a buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <param name="statuses">Optional list of statuses to filter by.</param>
    /// <param name="fromDate">Optional start date for date range filter (inclusive).</param>
    /// <param name="toDate">Optional end date for date range filter (inclusive).</param>
    /// <param name="storeId">Optional store ID to filter orders that contain sub-orders for this seller.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of orders for the current page and the total count.</returns>
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetFilteredByBuyerIdAsync(
        string buyerId,
        IReadOnlyList<OrderStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        Guid? storeId,
        int page,
        int pageSize);

    /// <summary>
    /// Gets distinct sellers (stores) from a buyer's orders.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A list of distinct store IDs and names.</returns>
    Task<IReadOnlyList<(Guid StoreId, string StoreName)>> GetDistinctSellersByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Gets a filtered and paginated list of all orders for admin view.
    /// </summary>
    /// <param name="statuses">Optional list of statuses to filter by.</param>
    /// <param name="fromDate">Optional start date for date range filter (inclusive).</param>
    /// <param name="toDate">Optional end date for date range filter (inclusive).</param>
    /// <param name="searchTerm">Optional search term for order number, buyer email, or store name.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of orders for the current page and the total count.</returns>
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetFilteredForAdminAsync(
        IReadOnlyList<OrderStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? searchTerm,
        int page,
        int pageSize);

    /// <summary>
    /// Adds a new order to the repository.
    /// </summary>
    /// <param name="order">The order to add.</param>
    /// <returns>The added order.</returns>
    Task<Order> AddAsync(Order order);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="order">The order to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Order order);
}
