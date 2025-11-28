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
