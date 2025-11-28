using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Queries;

namespace Mercato.Orders.Application.Services;

/// <summary>
/// Service interface for order management operations.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new order from the buyer's cart with validated items.
    /// </summary>
    /// <param name="command">The create order command.</param>
    /// <returns>The result of the create operation.</returns>
    Task<CreateOrderResult> CreateOrderAsync(CreateOrderCommand command);

    /// <summary>
    /// Gets an order by its unique identifier.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="buyerId">The buyer ID for authorization.</param>
    /// <returns>The order if found and authorized; otherwise, null.</returns>
    Task<GetOrderResult> GetOrderAsync(Guid orderId, string buyerId);

    /// <summary>
    /// Gets an order by payment transaction ID.
    /// </summary>
    /// <param name="transactionId">The payment transaction ID.</param>
    /// <param name="buyerId">The buyer ID for authorization.</param>
    /// <returns>The order if found and authorized; otherwise, null.</returns>
    Task<GetOrderResult> GetOrderByTransactionAsync(Guid transactionId, string buyerId);

    /// <summary>
    /// Updates the order status after payment completion.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="isPaymentSuccessful">Whether the payment was successful.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, bool isPaymentSuccessful);

    /// <summary>
    /// Gets all orders for a specific buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The result containing the buyer's orders.</returns>
    Task<GetOrdersResult> GetOrdersForBuyerAsync(string buyerId);

    /// <summary>
    /// Gets filtered and paginated orders for a specific buyer.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the filtered and paginated orders.</returns>
    Task<GetFilteredOrdersResult> GetFilteredOrdersForBuyerAsync(BuyerOrderFilterQuery query);

    /// <summary>
    /// Sends a confirmation email for an order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <returns>The result of the send operation.</returns>
    Task<SendEmailResult> SendOrderConfirmationEmailAsync(Guid orderId, string buyerEmail);

    /// <summary>
    /// Gets all seller sub-orders for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The result containing the store's sub-orders.</returns>
    Task<GetSellerSubOrdersResult> GetSellerSubOrdersAsync(Guid storeId);

    /// <summary>
    /// Gets a specific seller sub-order by its ID.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <returns>The result containing the seller sub-order.</returns>
    Task<GetSellerSubOrderResult> GetSellerSubOrderAsync(Guid subOrderId, Guid storeId);

    /// <summary>
    /// Updates the status of a seller sub-order.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <param name="command">The update command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateSellerSubOrderStatusResult> UpdateSellerSubOrderStatusAsync(
        Guid subOrderId,
        Guid storeId,
        UpdateSellerSubOrderStatusCommand command);
}
