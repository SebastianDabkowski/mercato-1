using System.Text;
using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Queries;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mercato.Orders.Infrastructure;

/// <summary>
/// Service implementation for order management operations.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ISellerSubOrderRepository _sellerSubOrderRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IShippingStatusHistoryRepository _shippingStatusHistoryRepository;
    private readonly IOrderConfirmationEmailService _emailService;
    private readonly IShippingNotificationService _shippingNotificationService;
    private readonly ReturnSettings _returnSettings;
    private readonly ILogger<OrderService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderService"/> class.
    /// </summary>
    /// <param name="orderRepository">The order repository.</param>
    /// <param name="sellerSubOrderRepository">The seller sub-order repository.</param>
    /// <param name="returnRequestRepository">The return request repository.</param>
    /// <param name="shippingStatusHistoryRepository">The shipping status history repository.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="shippingNotificationService">The shipping notification service.</param>
    /// <param name="returnSettings">The return settings.</param>
    /// <param name="logger">The logger.</param>
    public OrderService(
        IOrderRepository orderRepository,
        ISellerSubOrderRepository sellerSubOrderRepository,
        IReturnRequestRepository returnRequestRepository,
        IShippingStatusHistoryRepository shippingStatusHistoryRepository,
        IOrderConfirmationEmailService emailService,
        IShippingNotificationService shippingNotificationService,
        IOptions<ReturnSettings> returnSettings,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _sellerSubOrderRepository = sellerSubOrderRepository;
        _returnRequestRepository = returnRequestRepository;
        _shippingStatusHistoryRepository = shippingStatusHistoryRepository;
        _emailService = emailService;
        _shippingNotificationService = shippingNotificationService;
        _returnSettings = returnSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateOrderResult.Failure(validationErrors);
        }

        try
        {
            var orderId = Guid.NewGuid();
            var orderNumber = GenerateOrderNumber(orderId);
            var now = DateTimeOffset.UtcNow;

            var itemsSubtotal = command.Items.Sum(i => i.UnitPrice * i.Quantity);

            // Create the parent order
            var order = new Order
            {
                Id = orderId,
                BuyerId = command.BuyerId,
                OrderNumber = orderNumber,
                Status = OrderStatus.New,
                PaymentTransactionId = command.PaymentTransactionId,
                PaymentMethodName = command.PaymentMethodName,
                ItemsSubtotal = itemsSubtotal,
                ShippingTotal = command.ShippingTotal,
                TotalAmount = itemsSubtotal + command.ShippingTotal,
                DeliveryFullName = command.DeliveryAddress.FullName,
                DeliveryAddressLine1 = command.DeliveryAddress.AddressLine1,
                DeliveryAddressLine2 = command.DeliveryAddress.AddressLine2,
                DeliveryCity = command.DeliveryAddress.City,
                DeliveryState = command.DeliveryAddress.State,
                DeliveryPostalCode = command.DeliveryAddress.PostalCode,
                DeliveryCountry = command.DeliveryAddress.Country,
                DeliveryPhoneNumber = command.DeliveryAddress.PhoneNumber,
                BuyerEmail = command.BuyerEmail,
                DeliveryInstructions = command.DeliveryAddress.DeliveryInstructions,
                CreatedAt = now,
                LastUpdatedAt = now,
                Items = command.Items.Select(item => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = item.ProductId,
                    StoreId = item.StoreId,
                    ProductTitle = item.ProductTitle,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    StoreName = item.StoreName,
                    ProductImageUrl = item.ProductImageUrl,
                    CreatedAt = now
                }).ToList()
            };

            // Create seller sub-orders by grouping items by store
            // Shipping method is taken from the first item in each store group
            var itemsByStore = command.Items.GroupBy(i => new { i.StoreId, i.StoreName });
            var sellerCount = itemsByStore.Count();
            var shippingPerSeller = sellerCount > 0 ? command.ShippingTotal / sellerCount : 0;
            var subOrderIndex = 0;

            foreach (var storeGroup in itemsByStore)
            {
                subOrderIndex++;
                var subOrderId = Guid.NewGuid();
                var subOrderNumber = GenerateSubOrderNumber(orderNumber, subOrderIndex);
                var storeItemsSubtotal = storeGroup.Sum(i => i.UnitPrice * i.Quantity);
                var shippingMethodName = storeGroup.FirstOrDefault()?.ShippingMethodName;

                var sellerSubOrder = new SellerSubOrder
                {
                    Id = subOrderId,
                    OrderId = orderId,
                    StoreId = storeGroup.Key.StoreId,
                    StoreName = storeGroup.Key.StoreName,
                    SubOrderNumber = subOrderNumber,
                    Status = SellerSubOrderStatus.New,
                    ItemsSubtotal = storeItemsSubtotal,
                    ShippingCost = shippingPerSeller,
                    TotalAmount = storeItemsSubtotal + shippingPerSeller,
                    ShippingMethodName = shippingMethodName,
                    CreatedAt = now,
                    LastUpdatedAt = now,
                    Items = storeGroup.Select(item => new SellerSubOrderItem
                    {
                        Id = Guid.NewGuid(),
                        SellerSubOrderId = subOrderId,
                        ProductId = item.ProductId,
                        ProductTitle = item.ProductTitle,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        ProductImageUrl = item.ProductImageUrl,
                        CreatedAt = now,
                        Status = SellerSubOrderItemStatus.New,
                        LastUpdatedAt = now
                    }).ToList()
                };

                order.SellerSubOrders.Add(sellerSubOrder);
            }

            await _orderRepository.AddAsync(order);

            _logger.LogInformation(
                "Created order {OrderNumber} for buyer {BuyerId} with {ItemCount} items and {SubOrderCount} seller sub-orders, total {TotalAmount}",
                orderNumber, command.BuyerId, command.Items.Count, order.SellerSubOrders.Count, order.TotalAmount);

            return CreateOrderResult.Success(orderId, orderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for buyer {BuyerId}", command.BuyerId);
            return CreateOrderResult.Failure("An error occurred while creating the order.");
        }
    }

    /// <inheritdoc />
    public async Task<GetOrderResult> GetOrderAsync(Guid orderId, string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return GetOrderResult.Failure("Buyer ID is required.");
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return GetOrderResult.Failure("Order not found.");
            }

            if (order.BuyerId != buyerId)
            {
                return GetOrderResult.NotAuthorized();
            }

            return GetOrderResult.Success(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", orderId);
            return GetOrderResult.Failure("An error occurred while getting the order.");
        }
    }

    /// <inheritdoc />
    public async Task<GetOrderResult> GetOrderByTransactionAsync(Guid transactionId, string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return GetOrderResult.Failure("Buyer ID is required.");
        }

        try
        {
            var order = await _orderRepository.GetByPaymentTransactionIdAsync(transactionId);
            if (order == null)
            {
                return GetOrderResult.Failure("Order not found.");
            }

            if (order.BuyerId != buyerId)
            {
                return GetOrderResult.NotAuthorized();
            }

            return GetOrderResult.Success(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order by transaction {TransactionId}", transactionId);
            return GetOrderResult.Failure("An error occurred while getting the order.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, bool isPaymentSuccessful)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return UpdateOrderStatusResult.Failure("Order not found.");
            }

            // Validate that the order is in the correct state for payment processing
            if (order.Status != OrderStatus.New)
            {
                return UpdateOrderStatusResult.Failure($"Cannot process payment for order in status '{order.Status}'. Order must be in 'New' status.");
            }

            var now = DateTimeOffset.UtcNow;
            order.LastUpdatedAt = now;

            if (isPaymentSuccessful)
            {
                order.Status = OrderStatus.Paid;
                order.ConfirmedAt = now;

                // Update all seller sub-orders to Paid status
                foreach (var subOrder in order.SellerSubOrders)
                {
                    subOrder.Status = SellerSubOrderStatus.Paid;
                    subOrder.ConfirmedAt = now;
                    subOrder.LastUpdatedAt = now;
                }

                _logger.LogInformation("Order {OrderNumber} paid with {SubOrderCount} sub-orders", order.OrderNumber, order.SellerSubOrders.Count);
            }
            else
            {
                order.Status = OrderStatus.Failed;
                order.FailedAt = now;

                // Update all seller sub-orders to Failed status
                foreach (var subOrder in order.SellerSubOrders)
                {
                    subOrder.Status = SellerSubOrderStatus.Failed;
                    subOrder.FailedAt = now;
                    subOrder.LastUpdatedAt = now;
                }

                _logger.LogInformation("Order {OrderNumber} failed due to payment failure", order.OrderNumber);
            }

            await _orderRepository.UpdateAsync(order);

            return UpdateOrderStatusResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for {OrderId}", orderId);
            return UpdateOrderStatusResult.Failure("An error occurred while updating the order status.");
        }
    }

    /// <inheritdoc />
    public async Task<GetOrdersResult> GetOrdersForBuyerAsync(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return GetOrdersResult.Failure("Buyer ID is required.");
        }

        try
        {
            var orders = await _orderRepository.GetByBuyerIdAsync(buyerId);
            return GetOrdersResult.Success(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for buyer {BuyerId}", buyerId);
            return GetOrdersResult.Failure("An error occurred while getting the orders.");
        }
    }

    /// <inheritdoc />
    public async Task<GetFilteredOrdersResult> GetFilteredOrdersForBuyerAsync(BuyerOrderFilterQuery query)
    {
        var validationErrors = ValidateFilterQuery(query);
        if (validationErrors.Count > 0)
        {
            return GetFilteredOrdersResult.Failure(validationErrors);
        }

        try
        {
            var (orders, totalCount) = await _orderRepository.GetFilteredByBuyerIdAsync(
                query.BuyerId,
                query.Statuses.Count > 0 ? query.Statuses : null,
                query.FromDate,
                query.ToDate,
                query.StoreId,
                query.Page,
                query.PageSize);

            return GetFilteredOrdersResult.Success(orders, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered orders for buyer {BuyerId}", query.BuyerId);
            return GetFilteredOrdersResult.Failure("An error occurred while getting the orders.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Guid StoreId, string StoreName)>> GetDistinctSellersForBuyerAsync(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return [];
        }

        try
        {
            return await _orderRepository.GetDistinctSellersByBuyerIdAsync(buyerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distinct sellers for buyer {BuyerId}", buyerId);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<SendEmailResult> SendOrderConfirmationEmailAsync(Guid orderId, string buyerEmail)
    {
        if (string.IsNullOrEmpty(buyerEmail))
        {
            return SendEmailResult.Failure("Buyer email is required.");
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return SendEmailResult.Failure("Order not found.");
            }

            return await _emailService.SendOrderConfirmationAsync(order, buyerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending confirmation email for order {OrderId}", orderId);
            return SendEmailResult.Failure("An error occurred while sending the confirmation email.");
        }
    }

    /// <inheritdoc />
    public async Task<GetSellerSubOrdersResult> GetSellerSubOrdersAsync(Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            return GetSellerSubOrdersResult.Failure("Store ID is required.");
        }

        try
        {
            var subOrders = await _sellerSubOrderRepository.GetByStoreIdAsync(storeId);
            return GetSellerSubOrdersResult.Success(subOrders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seller sub-orders for store {StoreId}", storeId);
            return GetSellerSubOrdersResult.Failure("An error occurred while getting the sub-orders.");
        }
    }

    /// <inheritdoc />
    public async Task<GetSellerSubOrderResult> GetSellerSubOrderAsync(Guid subOrderId, Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            return GetSellerSubOrderResult.Failure("Store ID is required.");
        }

        try
        {
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(subOrderId);
            if (subOrder == null)
            {
                return GetSellerSubOrderResult.Failure("Sub-order not found.");
            }

            if (subOrder.StoreId != storeId)
            {
                return GetSellerSubOrderResult.NotAuthorized();
            }

            return GetSellerSubOrderResult.Success(subOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seller sub-order {SubOrderId}", subOrderId);
            return GetSellerSubOrderResult.Failure("An error occurred while getting the sub-order.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateSellerSubOrderStatusResult> UpdateSellerSubOrderStatusAsync(
        Guid subOrderId,
        Guid storeId,
        UpdateSellerSubOrderStatusCommand command)
    {
        if (storeId == Guid.Empty)
        {
            return UpdateSellerSubOrderStatusResult.Failure("Store ID is required.");
        }

        try
        {
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(subOrderId);
            if (subOrder == null)
            {
                return UpdateSellerSubOrderStatusResult.Failure("Sub-order not found.");
            }

            if (subOrder.StoreId != storeId)
            {
                return UpdateSellerSubOrderStatusResult.NotAuthorized();
            }

            var validationErrors = ValidateStatusTransition(subOrder.Status, command.NewStatus);
            if (validationErrors.Count > 0)
            {
                return UpdateSellerSubOrderStatusResult.Failure(validationErrors);
            }

            var previousStatus = subOrder.Status;
            var now = DateTimeOffset.UtcNow;
            subOrder.Status = command.NewStatus;
            subOrder.LastUpdatedAt = now;

            // Set status-specific timestamps and properties
            switch (command.NewStatus)
            {
                case SellerSubOrderStatus.Preparing:
                    break;
                case SellerSubOrderStatus.Shipped:
                    subOrder.ShippedAt = now;
                    subOrder.TrackingNumber = command.TrackingNumber;
                    subOrder.ShippingCarrier = command.ShippingCarrier;
                    break;
                case SellerSubOrderStatus.Delivered:
                    subOrder.DeliveredAt = now;
                    break;
                case SellerSubOrderStatus.Cancelled:
                    subOrder.CancelledAt = now;
                    break;
                case SellerSubOrderStatus.Refunded:
                    subOrder.RefundedAt = now;
                    // Update parent order if sub-order is refunded
                    await UpdateParentOrderForRefundAsync(subOrder, now);
                    break;
            }

            await _sellerSubOrderRepository.UpdateAsync(subOrder);

            // Record shipping status history for audit purposes
            await RecordShippingStatusHistoryAsync(
                subOrder.Id,
                previousStatus,
                command.NewStatus,
                now,
                command.TrackingNumber,
                command.ShippingCarrier,
                null);

            // Send shipping notification email when status changes to Shipped
            if (command.NewStatus == SellerSubOrderStatus.Shipped && subOrder.Order != null)
            {
                var emailResult = await _shippingNotificationService.SendShippingNotificationAsync(subOrder, subOrder.Order);
                if (!emailResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Failed to send shipping notification for sub-order {SubOrderNumber}: {Errors}",
                        subOrder.SubOrderNumber, string.Join(", ", emailResult.Errors));
                }
            }

            _logger.LogInformation(
                "Updated seller sub-order {SubOrderNumber} status from {PreviousStatus} to {Status}",
                subOrder.SubOrderNumber, previousStatus, command.NewStatus);

            return UpdateSellerSubOrderStatusResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating seller sub-order status for {SubOrderId}", subOrderId);
            return UpdateSellerSubOrderStatusResult.Failure("An error occurred while updating the sub-order status.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateTrackingInfoResult> UpdateTrackingInfoAsync(
        Guid subOrderId,
        Guid storeId,
        UpdateTrackingInfoCommand command)
    {
        if (storeId == Guid.Empty)
        {
            return UpdateTrackingInfoResult.Failure("Store ID is required.");
        }

        try
        {
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(subOrderId);
            if (subOrder == null)
            {
                return UpdateTrackingInfoResult.Failure("Sub-order not found.");
            }

            if (subOrder.StoreId != storeId)
            {
                return UpdateTrackingInfoResult.NotAuthorized();
            }

            // Tracking info can only be updated for shipped orders
            if (subOrder.Status != SellerSubOrderStatus.Shipped)
            {
                return UpdateTrackingInfoResult.Failure("Tracking information can only be updated for shipped orders.");
            }

            var now = DateTimeOffset.UtcNow;
            subOrder.TrackingNumber = command.TrackingNumber;
            subOrder.ShippingCarrier = command.ShippingCarrier;
            subOrder.LastUpdatedAt = now;

            await _sellerSubOrderRepository.UpdateAsync(subOrder);

            _logger.LogInformation(
                "Updated tracking info for seller sub-order {SubOrderNumber}: Carrier={Carrier}, TrackingNumber={TrackingNumber}",
                subOrder.SubOrderNumber, command.ShippingCarrier, command.TrackingNumber);

            return UpdateTrackingInfoResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tracking info for sub-order {SubOrderId}", subOrderId);
            return UpdateTrackingInfoResult.Failure("An error occurred while updating the tracking information.");
        }
    }

    /// <summary>
    /// Updates the parent order when a sub-order is refunded.
    /// Sets the order's RefundedAt timestamp and updates status to Refunded if all sub-orders are refunded.
    /// </summary>
    private async Task UpdateParentOrderForRefundAsync(SellerSubOrder refundedSubOrder, DateTimeOffset now)
    {
        var order = await _orderRepository.GetByIdAsync(refundedSubOrder.OrderId);
        if (order == null)
        {
            return;
        }

        // Check if all sub-orders are now refunded (including the current one being updated)
        var allSubOrdersRefunded = order.SellerSubOrders
            .Where(s => s.Id != refundedSubOrder.Id)
            .All(s => s.Status == SellerSubOrderStatus.Refunded);

        if (allSubOrdersRefunded)
        {
            order.Status = OrderStatus.Refunded;
            order.RefundedAt = now;
            order.LastUpdatedAt = now;
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order {OrderNumber} fully refunded", order.OrderNumber);
        }
    }

    /// <summary>
    /// Records a shipping status change in the history for audit purposes.
    /// </summary>
    private async Task RecordShippingStatusHistoryAsync(
        Guid sellerSubOrderId,
        SellerSubOrderStatus? previousStatus,
        SellerSubOrderStatus newStatus,
        DateTimeOffset changedAt,
        string? trackingNumber,
        string? shippingCarrier,
        string? notes)
    {
        try
        {
            var history = new ShippingStatusHistory
            {
                Id = Guid.NewGuid(),
                SellerSubOrderId = sellerSubOrderId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                ChangedAt = changedAt,
                TrackingNumber = trackingNumber,
                ShippingCarrier = shippingCarrier,
                Notes = notes
            };

            await _shippingStatusHistoryRepository.AddAsync(history);

            _logger.LogDebug(
                "Recorded shipping status history for sub-order {SubOrderId}: {PreviousStatus} -> {NewStatus}",
                sellerSubOrderId, previousStatus, newStatus);
        }
        catch (Exception ex)
        {
            // Log but don't fail the main operation if history recording fails
            _logger.LogWarning(ex,
                "Failed to record shipping status history for sub-order {SubOrderId}",
                sellerSubOrderId);
        }
    }

    private static string GenerateOrderNumber(Guid orderId)
    {
        return $"ORD-{orderId.ToString("N")[..8].ToUpperInvariant()}";
    }

    private static string GenerateSubOrderNumber(string orderNumber, int subOrderIndex)
    {
        return $"{orderNumber}-S{subOrderIndex}";
    }

    private static List<string> ValidateCreateCommand(CreateOrderCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (command.PaymentTransactionId == Guid.Empty)
        {
            errors.Add("Payment transaction ID is required.");
        }

        if (command.Items == null || command.Items.Count == 0)
        {
            errors.Add("Order must contain at least one item.");
        }

        if (command.DeliveryAddress == null)
        {
            errors.Add("Delivery address is required.");
        }
        else
        {
            if (string.IsNullOrEmpty(command.DeliveryAddress.FullName))
            {
                errors.Add("Delivery full name is required.");
            }
            if (string.IsNullOrEmpty(command.DeliveryAddress.AddressLine1))
            {
                errors.Add("Delivery address line 1 is required.");
            }
            if (string.IsNullOrEmpty(command.DeliveryAddress.City))
            {
                errors.Add("Delivery city is required.");
            }
            if (string.IsNullOrEmpty(command.DeliveryAddress.PostalCode))
            {
                errors.Add("Delivery postal code is required.");
            }
            if (string.IsNullOrEmpty(command.DeliveryAddress.Country))
            {
                errors.Add("Delivery country is required.");
            }
        }

        return errors;
    }

    private static List<string> ValidateStatusTransition(SellerSubOrderStatus currentStatus, SellerSubOrderStatus newStatus)
    {
        var errors = new List<string>();

        // Define valid status transitions for seller-initiated actions.
        // Note: Refunded status can only be reached from Paid, Delivered, or Cancelled states
        // and is handled by admin/support workflow, but sellers can initiate the transition.
        var validTransitions = new Dictionary<SellerSubOrderStatus, SellerSubOrderStatus[]>
        {
            { SellerSubOrderStatus.New, [SellerSubOrderStatus.Paid, SellerSubOrderStatus.Cancelled] },
            { SellerSubOrderStatus.Paid, [SellerSubOrderStatus.Preparing, SellerSubOrderStatus.Cancelled, SellerSubOrderStatus.Refunded] },
            { SellerSubOrderStatus.Preparing, [SellerSubOrderStatus.Shipped, SellerSubOrderStatus.Cancelled] },
            { SellerSubOrderStatus.Shipped, [SellerSubOrderStatus.Delivered] },
            { SellerSubOrderStatus.Delivered, [SellerSubOrderStatus.Refunded] },
            { SellerSubOrderStatus.Cancelled, [SellerSubOrderStatus.Refunded] },
            { SellerSubOrderStatus.Refunded, [] },
            { SellerSubOrderStatus.Failed, [] }
        };

        if (!validTransitions.TryGetValue(currentStatus, out var allowedStatuses) ||
            !allowedStatuses.Contains(newStatus))
        {
            errors.Add($"Cannot transition from {currentStatus} to {newStatus}.");
        }

        return errors;
    }

    private static List<string> ValidateFilterQuery(BuyerOrderFilterQuery query)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(query.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (query.Page < 1)
        {
            errors.Add("Page number must be at least 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            errors.Add("Page size must be between 1 and 100.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            errors.Add("From date cannot be after to date.");
        }

        return errors;
    }

    /// <inheritdoc />
    public async Task<GetFilteredSellerSubOrdersResult> GetFilteredSellerSubOrdersAsync(SellerSubOrderFilterQuery query)
    {
        var validationErrors = ValidateSellerSubOrderFilterQuery(query);
        if (validationErrors.Count > 0)
        {
            return GetFilteredSellerSubOrdersResult.Failure(validationErrors);
        }

        try
        {
            var (subOrders, totalCount) = await _sellerSubOrderRepository.GetFilteredByStoreIdAsync(
                query.StoreId,
                query.Statuses.Count > 0 ? query.Statuses : null,
                query.FromDate,
                query.ToDate,
                query.BuyerSearchTerm,
                query.Page,
                query.PageSize);

            return GetFilteredSellerSubOrdersResult.Success(subOrders, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered sub-orders for store {StoreId}", query.StoreId);
            return GetFilteredSellerSubOrdersResult.Failure("An error occurred while getting the sub-orders.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(string BuyerId, string BuyerEmail)>> GetDistinctBuyersForStoreAsync(Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            return [];
        }

        try
        {
            return await _sellerSubOrderRepository.GetDistinctBuyersByStoreIdAsync(storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distinct buyers for store {StoreId}", storeId);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportSellerSubOrdersToCsvAsync(Guid storeId, SellerSubOrderFilterQuery query)
    {
        // Create a new query object to avoid mutating the input parameter
        var exportQuery = new SellerSubOrderFilterQuery
        {
            StoreId = storeId,
            Statuses = query.Statuses,
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            BuyerSearchTerm = query.BuyerSearchTerm,
            Page = 1,
            PageSize = 10000 // Use a larger page size for export with reasonable limit
        };

        var validationErrors = ValidateSellerSubOrderFilterQuery(exportQuery);
        if (validationErrors.Count > 0)
        {
            _logger.LogWarning("Export validation failed for store {StoreId}: {Errors}", storeId, string.Join(", ", validationErrors));
            return [];
        }

        try
        {
            var (subOrders, _) = await _sellerSubOrderRepository.GetFilteredByStoreIdAsync(
                exportQuery.StoreId,
                exportQuery.Statuses.Count > 0 ? exportQuery.Statuses : null,
                exportQuery.FromDate,
                exportQuery.ToDate,
                exportQuery.BuyerSearchTerm,
                exportQuery.Page,
                exportQuery.PageSize);

            // Return empty if no orders to export
            if (subOrders.Count == 0)
            {
                return [];
            }

            var csv = new StringBuilder();

            // CSV Header with key shipping fields for logistics partners
            // Structure documented for external logistics systems
            csv.AppendLine("Sub-Order Number,Order Number,Creation Date,Status,Buyer Name,Delivery Address Line 1,Delivery Address Line 2,City,State,Postal Code,Country,Phone,Shipping Method,Tracking Number,Shipping Carrier,Items,Total Amount");

            // CSV Data
            foreach (var subOrder in subOrders)
            {
                var order = subOrder.Order;
                var items = subOrder.Items ?? [];
                var itemsSummary = string.Join("; ", items.Select(i => $"{i.ProductTitle} x{i.Quantity}"));

                var line = string.Join(",",
                    EscapeCsvField(subOrder.SubOrderNumber),
                    EscapeCsvField(order?.OrderNumber ?? string.Empty),
                    EscapeCsvField(subOrder.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsvField(subOrder.Status.ToString()),
                    EscapeCsvField(order?.DeliveryFullName ?? string.Empty),
                    EscapeCsvField(order?.DeliveryAddressLine1 ?? string.Empty),
                    EscapeCsvField(order?.DeliveryAddressLine2 ?? string.Empty),
                    EscapeCsvField(order?.DeliveryCity ?? string.Empty),
                    EscapeCsvField(order?.DeliveryState ?? string.Empty),
                    EscapeCsvField(order?.DeliveryPostalCode ?? string.Empty),
                    EscapeCsvField(order?.DeliveryCountry ?? string.Empty),
                    EscapeCsvField(order?.DeliveryPhoneNumber ?? string.Empty),
                    EscapeCsvField(subOrder.ShippingMethodName ?? string.Empty),
                    EscapeCsvField(subOrder.TrackingNumber ?? string.Empty),
                    EscapeCsvField(subOrder.ShippingCarrier ?? string.Empty),
                    EscapeCsvField(itemsSummary),
                    EscapeCsvField(subOrder.TotalAmount.ToString("F2")));

                csv.AppendLine(line);
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sub-orders to CSV for store {StoreId}", storeId);
            return [];
        }
    }

    private static List<string> ValidateSellerSubOrderFilterQuery(SellerSubOrderFilterQuery query)
    {
        var errors = new List<string>();

        if (query.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (query.Page < 1)
        {
            errors.Add("Page number must be at least 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 10000)
        {
            errors.Add("Page size must be between 1 and 10000.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            errors.Add("From date cannot be after to date.");
        }

        return errors;
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // If the field contains special characters, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    /// <inheritdoc />
    public async Task<CreateReturnRequestResult> CreateReturnRequestAsync(CreateReturnRequestCommand command)
    {
        var validationErrors = ValidateCreateReturnRequestCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateReturnRequestResult.Failure(validationErrors);
        }

        try
        {
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(command.SellerSubOrderId);
            if (subOrder == null)
            {
                return CreateReturnRequestResult.Failure("Sub-order not found.");
            }

            // Check authorization - buyer must own the parent order
            if (subOrder.Order == null || subOrder.Order.BuyerId != command.BuyerId)
            {
                return CreateReturnRequestResult.NotAuthorized();
            }

            // Check if sub-order is delivered
            if (subOrder.Status != SellerSubOrderStatus.Delivered)
            {
                return CreateReturnRequestResult.Failure("Return can only be initiated for delivered orders.");
            }

            // Check return window
            if (!IsWithinReturnWindow(subOrder.DeliveredAt))
            {
                return CreateReturnRequestResult.Failure(
                    $"Return window has expired. Returns must be initiated within {_returnSettings.ReturnWindowDays} days of delivery.");
            }

            // Check if a return request already exists
            var existingRequest = await _returnRequestRepository.GetBySellerSubOrderIdAsync(command.SellerSubOrderId);
            if (existingRequest != null)
            {
                return CreateReturnRequestResult.Failure("A return request already exists for this sub-order.");
            }

            var now = DateTimeOffset.UtcNow;
            var returnRequest = new ReturnRequest
            {
                Id = Guid.NewGuid(),
                SellerSubOrderId = command.SellerSubOrderId,
                BuyerId = command.BuyerId,
                Status = ReturnStatus.Requested,
                Reason = command.Reason,
                CreatedAt = now,
                LastUpdatedAt = now
            };

            await _returnRequestRepository.AddAsync(returnRequest);

            _logger.LogInformation(
                "Created return request {ReturnRequestId} for sub-order {SubOrderNumber} by buyer {BuyerId}",
                returnRequest.Id, subOrder.SubOrderNumber, command.BuyerId);

            return CreateReturnRequestResult.Success(returnRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating return request for sub-order {SubOrderId}", command.SellerSubOrderId);
            return CreateReturnRequestResult.Failure("An error occurred while creating the return request.");
        }
    }

    /// <inheritdoc />
    public async Task<GetReturnRequestResult> GetReturnRequestAsync(Guid returnRequestId, string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return GetReturnRequestResult.Failure("Buyer ID is required.");
        }

        try
        {
            var returnRequest = await _returnRequestRepository.GetByIdAsync(returnRequestId);
            if (returnRequest == null)
            {
                return GetReturnRequestResult.Failure("Return request not found.");
            }

            if (returnRequest.BuyerId != buyerId)
            {
                return GetReturnRequestResult.NotAuthorized();
            }

            return GetReturnRequestResult.Success(returnRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting return request {ReturnRequestId}", returnRequestId);
            return GetReturnRequestResult.Failure("An error occurred while getting the return request.");
        }
    }

    /// <inheritdoc />
    public async Task<GetReturnRequestsResult> GetReturnRequestsForBuyerAsync(string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return GetReturnRequestsResult.Failure("Buyer ID is required.");
        }

        try
        {
            var returnRequests = await _returnRequestRepository.GetByBuyerIdAsync(buyerId);
            return GetReturnRequestsResult.Success(returnRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting return requests for buyer {BuyerId}", buyerId);
            return GetReturnRequestsResult.Failure("An error occurred while getting the return requests.");
        }
    }

    /// <inheritdoc />
    public async Task<GetReturnRequestResult> GetReturnRequestForSellerSubOrderAsync(Guid subOrderId, Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            return GetReturnRequestResult.Failure("Store ID is required.");
        }

        try
        {
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(subOrderId);
            if (subOrder == null)
            {
                return GetReturnRequestResult.Failure("Sub-order not found.");
            }

            if (subOrder.StoreId != storeId)
            {
                return GetReturnRequestResult.NotAuthorized();
            }

            var returnRequest = await _returnRequestRepository.GetBySellerSubOrderIdAsync(subOrderId);
            if (returnRequest == null)
            {
                return GetReturnRequestResult.Failure("Return request not found.");
            }

            return GetReturnRequestResult.Success(returnRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting return request for sub-order {SubOrderId}", subOrderId);
            return GetReturnRequestResult.Failure("An error occurred while getting the return request.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateReturnRequestStatusResult> UpdateReturnRequestStatusAsync(
        Guid returnRequestId,
        Guid storeId,
        UpdateReturnRequestStatusCommand command)
    {
        if (storeId == Guid.Empty)
        {
            return UpdateReturnRequestStatusResult.Failure("Store ID is required.");
        }

        try
        {
            var returnRequest = await _returnRequestRepository.GetByIdAsync(returnRequestId);
            if (returnRequest == null)
            {
                return UpdateReturnRequestStatusResult.Failure("Return request not found.");
            }

            // Check authorization - store must own the sub-order
            if (returnRequest.SellerSubOrder == null || returnRequest.SellerSubOrder.StoreId != storeId)
            {
                return UpdateReturnRequestStatusResult.NotAuthorized();
            }

            var validationErrors = ValidateReturnStatusTransition(returnRequest.Status, command.NewStatus);
            if (validationErrors.Count > 0)
            {
                return UpdateReturnRequestStatusResult.Failure(validationErrors);
            }

            var now = DateTimeOffset.UtcNow;
            returnRequest.Status = command.NewStatus;
            returnRequest.LastUpdatedAt = now;
            returnRequest.SellerNotes = command.SellerNotes;

            await _returnRequestRepository.UpdateAsync(returnRequest);

            _logger.LogInformation(
                "Updated return request {ReturnRequestId} status to {Status}",
                returnRequestId, command.NewStatus);

            return UpdateReturnRequestStatusResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating return request status for {ReturnRequestId}", returnRequestId);
            return UpdateReturnRequestStatusResult.Failure("An error occurred while updating the return request status.");
        }
    }

    /// <inheritdoc />
    public async Task<CanInitiateReturnResult> CanInitiateReturnAsync(Guid subOrderId, string buyerId)
    {
        if (string.IsNullOrEmpty(buyerId))
        {
            return CanInitiateReturnResult.Failure("Buyer ID is required.");
        }

        try
        {
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(subOrderId);
            if (subOrder == null)
            {
                return CanInitiateReturnResult.Failure("Sub-order not found.");
            }

            // Check authorization - buyer must own the parent order
            if (subOrder.Order == null || subOrder.Order.BuyerId != buyerId)
            {
                return CanInitiateReturnResult.NotAuthorized();
            }

            // Check if sub-order is delivered
            if (subOrder.Status != SellerSubOrderStatus.Delivered)
            {
                return CanInitiateReturnResult.No("Return can only be initiated for delivered orders.");
            }

            // Check return window
            if (!IsWithinReturnWindow(subOrder.DeliveredAt))
            {
                return CanInitiateReturnResult.No(
                    $"Return window has expired. Returns must be initiated within {_returnSettings.ReturnWindowDays} days of delivery.");
            }

            // Check if a return request already exists
            var existingRequest = await _returnRequestRepository.GetBySellerSubOrderIdAsync(subOrderId);
            if (existingRequest != null)
            {
                return CanInitiateReturnResult.No("A return request already exists for this sub-order.");
            }

            return CanInitiateReturnResult.Yes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking return eligibility for sub-order {SubOrderId}", subOrderId);
            return CanInitiateReturnResult.Failure("An error occurred while checking return eligibility.");
        }
    }

    /// <inheritdoc />
    public async Task<UpdateSubOrderItemStatusResult> UpdateSubOrderItemStatusAsync(
        Guid subOrderId,
        Guid storeId,
        UpdateSubOrderItemStatusCommand command)
    {
        if (storeId == Guid.Empty)
        {
            return UpdateSubOrderItemStatusResult.Failure("Store ID is required.");
        }

        var validationErrors = ValidateItemStatusCommand(command);
        if (validationErrors.Count > 0)
        {
            return UpdateSubOrderItemStatusResult.Failure(validationErrors);
        }

        try
        {
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(subOrderId);
            if (subOrder == null)
            {
                return UpdateSubOrderItemStatusResult.Failure("Sub-order not found.");
            }

            if (subOrder.StoreId != storeId)
            {
                return UpdateSubOrderItemStatusResult.NotAuthorized();
            }

            // Validate sub-order is in a valid state for item updates
            if (subOrder.Status != SellerSubOrderStatus.Paid && subOrder.Status != SellerSubOrderStatus.Preparing)
            {
                return UpdateSubOrderItemStatusResult.Failure(
                    "Item statuses can only be updated when sub-order is in Paid or Preparing status.");
            }

            var now = DateTimeOffset.UtcNow;
            var itemDict = subOrder.Items.ToDictionary(i => i.Id);

            foreach (var update in command.ItemUpdates)
            {
                if (!itemDict.TryGetValue(update.ItemId, out var item))
                {
                    return UpdateSubOrderItemStatusResult.Failure($"Item {update.ItemId} not found in sub-order.");
                }

                var itemTransitionErrors = ValidateItemStatusTransition(item.Status, update.NewStatus);
                if (itemTransitionErrors.Count > 0)
                {
                    return UpdateSubOrderItemStatusResult.Failure(itemTransitionErrors);
                }

                item.Status = update.NewStatus;
                item.LastUpdatedAt = now;

                switch (update.NewStatus)
                {
                    case SellerSubOrderItemStatus.Shipped:
                        item.ShippedAt = now;
                        break;
                    case SellerSubOrderItemStatus.Delivered:
                        item.DeliveredAt = now;
                        break;
                    case SellerSubOrderItemStatus.Cancelled:
                        item.CancelledAt = now;
                        break;
                }
            }

            // Update sub-order status based on item statuses
            UpdateSubOrderStatusFromItems(subOrder, now, command.TrackingNumber, command.ShippingCarrier);

            await _sellerSubOrderRepository.UpdateAsync(subOrder);

            _logger.LogInformation(
                "Updated {ItemCount} item(s) in seller sub-order {SubOrderNumber}",
                command.ItemUpdates.Count, subOrder.SubOrderNumber);

            return UpdateSubOrderItemStatusResult.Success(subOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item statuses for sub-order {SubOrderId}", subOrderId);
            return UpdateSubOrderItemStatusResult.Failure("An error occurred while updating item statuses.");
        }
    }

    /// <inheritdoc />
    public async Task<CalculateItemRefundResult> CalculateCancelledItemsRefundAsync(Guid subOrderId, Guid storeId)
    {
        if (storeId == Guid.Empty)
        {
            return CalculateItemRefundResult.Failure("Store ID is required.");
        }

        try
        {
            var subOrder = await _sellerSubOrderRepository.GetByIdAsync(subOrderId);
            if (subOrder == null)
            {
                return CalculateItemRefundResult.Failure("Sub-order not found.");
            }

            if (subOrder.StoreId != storeId)
            {
                return CalculateItemRefundResult.NotAuthorized();
            }

            var cancelledItems = subOrder.Items
                .Where(i => i.Status == SellerSubOrderItemStatus.Cancelled)
                .Select(i => new CancelledItemDetail
                {
                    ItemId = i.Id,
                    ProductTitle = i.ProductTitle,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    RefundAmount = i.TotalPrice,
                    CancelledAt = i.CancelledAt
                })
                .ToList();

            var totalRefundAmount = cancelledItems.Sum(i => i.RefundAmount);

            _logger.LogInformation(
                "Calculated refund of {RefundAmount} for {ItemCount} cancelled item(s) in sub-order {SubOrderNumber}",
                totalRefundAmount, cancelledItems.Count, subOrder.SubOrderNumber);

            return CalculateItemRefundResult.Success(totalRefundAmount, cancelledItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating refund for sub-order {SubOrderId}", subOrderId);
            return CalculateItemRefundResult.Failure("An error occurred while calculating the refund.");
        }
    }

    /// <summary>
    /// Updates the sub-order status based on item statuses.
    /// </summary>
    private static void UpdateSubOrderStatusFromItems(
        SellerSubOrder subOrder,
        DateTimeOffset now,
        string? trackingNumber,
        string? shippingCarrier)
    {
        var items = subOrder.Items.ToList();

        // If all items are cancelled, cancel the sub-order
        if (items.All(i => i.Status == SellerSubOrderItemStatus.Cancelled))
        {
            subOrder.Status = SellerSubOrderStatus.Cancelled;
            subOrder.CancelledAt = now;
            subOrder.LastUpdatedAt = now;
            return;
        }

        // If all non-cancelled items are delivered, mark as delivered
        var nonCancelledItems = items.Where(i => i.Status != SellerSubOrderItemStatus.Cancelled).ToList();
        if (nonCancelledItems.All(i => i.Status == SellerSubOrderItemStatus.Delivered))
        {
            subOrder.Status = SellerSubOrderStatus.Delivered;
            subOrder.DeliveredAt = now;
            subOrder.LastUpdatedAt = now;
            return;
        }

        // If any item is shipped or delivered, mark sub-order as shipped
        if (nonCancelledItems.Any(i => i.Status == SellerSubOrderItemStatus.Shipped || i.Status == SellerSubOrderItemStatus.Delivered))
        {
            if (subOrder.Status != SellerSubOrderStatus.Shipped)
            {
                subOrder.Status = SellerSubOrderStatus.Shipped;
                subOrder.ShippedAt = now;
                subOrder.TrackingNumber = trackingNumber;
                subOrder.ShippingCarrier = shippingCarrier;
                subOrder.LastUpdatedAt = now;
            }
            return;
        }

        // If any item is preparing, mark sub-order as preparing
        if (items.Any(i => i.Status == SellerSubOrderItemStatus.Preparing))
        {
            if (subOrder.Status != SellerSubOrderStatus.Preparing)
            {
                subOrder.Status = SellerSubOrderStatus.Preparing;
                subOrder.LastUpdatedAt = now;
            }
        }
    }

    private static List<string> ValidateItemStatusCommand(UpdateSubOrderItemStatusCommand command)
    {
        var errors = new List<string>();

        if (command.ItemUpdates == null || command.ItemUpdates.Count == 0)
        {
            errors.Add("At least one item update is required.");
            return errors;
        }

        foreach (var update in command.ItemUpdates)
        {
            if (update.ItemId == Guid.Empty)
            {
                errors.Add("Item ID is required for each update.");
            }
        }

        return errors;
    }

    private static List<string> ValidateItemStatusTransition(SellerSubOrderItemStatus currentStatus, SellerSubOrderItemStatus newStatus)
    {
        var errors = new List<string>();

        // Define valid status transitions for item-level statuses
        var validTransitions = new Dictionary<SellerSubOrderItemStatus, SellerSubOrderItemStatus[]>
        {
            { SellerSubOrderItemStatus.New, [SellerSubOrderItemStatus.Preparing, SellerSubOrderItemStatus.Shipped, SellerSubOrderItemStatus.Cancelled] },
            { SellerSubOrderItemStatus.Preparing, [SellerSubOrderItemStatus.Shipped, SellerSubOrderItemStatus.Cancelled] },
            { SellerSubOrderItemStatus.Shipped, [SellerSubOrderItemStatus.Delivered] },
            { SellerSubOrderItemStatus.Delivered, [] },
            { SellerSubOrderItemStatus.Cancelled, [] }
        };

        if (!validTransitions.TryGetValue(currentStatus, out var allowedStatuses) ||
            !allowedStatuses.Contains(newStatus))
        {
            errors.Add($"Cannot transition item from {currentStatus} to {newStatus}.");
        }

        return errors;
    }

    private bool IsWithinReturnWindow(DateTimeOffset? deliveredAt)
    {
        if (!deliveredAt.HasValue)
        {
            return false;
        }

        var returnDeadline = deliveredAt.Value.AddDays(_returnSettings.ReturnWindowDays);
        return DateTimeOffset.UtcNow <= returnDeadline;
    }

    private static List<string> ValidateCreateReturnRequestCommand(CreateReturnRequestCommand command)
    {
        var errors = new List<string>();

        if (command.SellerSubOrderId == Guid.Empty)
        {
            errors.Add("Seller sub-order ID is required.");
        }

        if (string.IsNullOrEmpty(command.BuyerId))
        {
            errors.Add("Buyer ID is required.");
        }

        if (string.IsNullOrEmpty(command.Reason))
        {
            errors.Add("Return reason is required.");
        }
        else if (command.Reason.Length > 2000)
        {
            errors.Add("Return reason must not exceed 2000 characters.");
        }

        return errors;
    }

    private static List<string> ValidateReturnStatusTransition(ReturnStatus currentStatus, ReturnStatus newStatus)
    {
        var errors = new List<string>();

        // Define valid status transitions
        var validTransitions = new Dictionary<ReturnStatus, ReturnStatus[]>
        {
            { ReturnStatus.Requested, [ReturnStatus.UnderReview, ReturnStatus.Approved, ReturnStatus.Rejected] },
            { ReturnStatus.UnderReview, [ReturnStatus.Approved, ReturnStatus.Rejected] },
            { ReturnStatus.Approved, [ReturnStatus.Completed] },
            { ReturnStatus.Rejected, [] },
            { ReturnStatus.Completed, [] }
        };

        if (!validTransitions.TryGetValue(currentStatus, out var allowedStatuses) ||
            !allowedStatuses.Contains(newStatus))
        {
            errors.Add($"Cannot transition from {currentStatus} to {newStatus}.");
        }

        return errors;
    }

    /// <inheritdoc />
    public async Task<GetAdminOrdersResult> GetAdminOrdersAsync(AdminOrderFilterQuery query)
    {
        var validationErrors = ValidateAdminOrderFilterQuery(query);
        if (validationErrors.Count > 0)
        {
            return GetAdminOrdersResult.Failure(validationErrors);
        }

        try
        {
            var (orders, totalCount) = await _orderRepository.GetFilteredForAdminAsync(
                query.Statuses.Count > 0 ? query.Statuses : null,
                query.FromDate,
                query.ToDate,
                query.SearchTerm,
                query.Page,
                query.PageSize);

            return GetAdminOrdersResult.Success(orders, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin orders");
            return GetAdminOrdersResult.Failure("An error occurred while getting the orders.");
        }
    }

    /// <inheritdoc />
    public async Task<GetOrderResult> GetOrderForAdminAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            return GetOrderResult.Failure("Order ID is required.");
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return GetOrderResult.Failure("Order not found.");
            }

            return GetOrderResult.Success(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId} for admin", orderId);
            return GetOrderResult.Failure("An error occurred while getting the order.");
        }
    }

    /// <inheritdoc />
    public async Task<GetShippingStatusHistoryResult> GetShippingStatusHistoryAsync(Guid sellerSubOrderId)
    {
        if (sellerSubOrderId == Guid.Empty)
        {
            return GetShippingStatusHistoryResult.Failure("Seller sub-order ID is required.");
        }

        try
        {
            var history = await _shippingStatusHistoryRepository.GetBySellerSubOrderIdAsync(sellerSubOrderId);
            return GetShippingStatusHistoryResult.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shipping status history for sub-order {SubOrderId}", sellerSubOrderId);
            return GetShippingStatusHistoryResult.Failure("An error occurred while getting the shipping status history.");
        }
    }

    private static List<string> ValidateAdminOrderFilterQuery(AdminOrderFilterQuery query)
    {
        var errors = new List<string>();

        if (query.Page < 1)
        {
            errors.Add("Page number must be at least 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            errors.Add("Page size must be between 1 and 100.");
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            errors.Add("From date cannot be after to date.");
        }

        return errors;
    }
}
