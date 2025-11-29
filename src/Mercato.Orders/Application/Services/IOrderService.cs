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
    /// Gets distinct sellers from a buyer's orders for filter dropdowns.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A list of distinct store IDs and names.</returns>
    Task<IReadOnlyList<(Guid StoreId, string StoreName)>> GetDistinctSellersForBuyerAsync(string buyerId);

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

    /// <summary>
    /// Updates the tracking information for a shipped seller sub-order without changing status.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <param name="command">The tracking info update command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateTrackingInfoResult> UpdateTrackingInfoAsync(
        Guid subOrderId,
        Guid storeId,
        UpdateTrackingInfoCommand command);

    /// <summary>
    /// Gets filtered and paginated seller sub-orders for a specific store.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the filtered and paginated sub-orders.</returns>
    Task<GetFilteredSellerSubOrdersResult> GetFilteredSellerSubOrdersAsync(SellerSubOrderFilterQuery query);

    /// <summary>
    /// Gets distinct buyers from a store's sub-orders for filter dropdowns.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of distinct buyer IDs and emails.</returns>
    Task<IReadOnlyList<(string BuyerId, string BuyerEmail)>> GetDistinctBuyersForStoreAsync(Guid storeId);

    /// <summary>
    /// Exports seller sub-orders to CSV format.
    /// </summary>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The CSV file as a byte array.</returns>
    Task<byte[]> ExportSellerSubOrdersToCsvAsync(Guid storeId, SellerSubOrderFilterQuery query);

    /// <summary>
    /// Creates a new return request for a seller sub-order.
    /// </summary>
    /// <param name="command">The create return request command.</param>
    /// <returns>The result of the create operation.</returns>
    Task<CreateReturnRequestResult> CreateReturnRequestAsync(CreateReturnRequestCommand command);

    /// <summary>
    /// Gets a return request by its unique identifier.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="buyerId">The buyer ID for authorization.</param>
    /// <returns>The return request if found and authorized.</returns>
    Task<GetReturnRequestResult> GetReturnRequestAsync(Guid returnRequestId, string buyerId);

    /// <summary>
    /// Gets all return requests for a specific buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The result containing the buyer's return requests.</returns>
    Task<GetReturnRequestsResult> GetReturnRequestsForBuyerAsync(string buyerId);

    /// <summary>
    /// Gets the return request for a specific seller sub-order.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <returns>The return request if found and authorized.</returns>
    Task<GetReturnRequestResult> GetReturnRequestForSellerSubOrderAsync(Guid subOrderId, Guid storeId);

    /// <summary>
    /// Updates the status of a return request.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <param name="command">The update command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateReturnRequestStatusResult> UpdateReturnRequestStatusAsync(
        Guid returnRequestId,
        Guid storeId,
        UpdateReturnRequestStatusCommand command);

    /// <summary>
    /// Checks whether a return can be initiated for a seller sub-order.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <param name="buyerId">The buyer ID for authorization.</param>
    /// <returns>The result indicating whether a return can be initiated.</returns>
    Task<CanInitiateReturnResult> CanInitiateReturnAsync(Guid subOrderId, string buyerId);

    /// <summary>
    /// Updates the status of individual items within a seller sub-order.
    /// Enables partial fulfillment by allowing sellers to ship, prepare, or cancel specific items.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <param name="command">The update command with item status updates.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateSubOrderItemStatusResult> UpdateSubOrderItemStatusAsync(
        Guid subOrderId,
        Guid storeId,
        UpdateSubOrderItemStatusCommand command);

    /// <summary>
    /// Calculates the refund amount for cancelled items within a seller sub-order.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <returns>The result containing the refund amount and cancelled item details.</returns>
    Task<CalculateItemRefundResult> CalculateCancelledItemsRefundAsync(Guid subOrderId, Guid storeId);
}
