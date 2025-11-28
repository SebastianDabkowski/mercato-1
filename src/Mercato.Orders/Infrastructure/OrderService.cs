using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Orders.Infrastructure;

/// <summary>
/// Service implementation for order management operations.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ISellerSubOrderRepository _sellerSubOrderRepository;
    private readonly IOrderConfirmationEmailService _emailService;
    private readonly ILogger<OrderService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderService"/> class.
    /// </summary>
    /// <param name="orderRepository">The order repository.</param>
    /// <param name="sellerSubOrderRepository">The seller sub-order repository.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="logger">The logger.</param>
    public OrderService(
        IOrderRepository orderRepository,
        ISellerSubOrderRepository sellerSubOrderRepository,
        IOrderConfirmationEmailService emailService,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _sellerSubOrderRepository = sellerSubOrderRepository;
        _emailService = emailService;
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
                Status = OrderStatus.Pending,
                PaymentTransactionId = command.PaymentTransactionId,
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

                var sellerSubOrder = new SellerSubOrder
                {
                    Id = subOrderId,
                    OrderId = orderId,
                    StoreId = storeGroup.Key.StoreId,
                    StoreName = storeGroup.Key.StoreName,
                    SubOrderNumber = subOrderNumber,
                    Status = SellerSubOrderStatus.Pending,
                    ItemsSubtotal = storeItemsSubtotal,
                    ShippingCost = shippingPerSeller,
                    TotalAmount = storeItemsSubtotal + shippingPerSeller,
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
                        CreatedAt = now
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

            var now = DateTimeOffset.UtcNow;
            order.LastUpdatedAt = now;

            if (isPaymentSuccessful)
            {
                order.Status = OrderStatus.Confirmed;
                order.ConfirmedAt = now;

                // Update all seller sub-orders to Confirmed status
                foreach (var subOrder in order.SellerSubOrders)
                {
                    subOrder.Status = SellerSubOrderStatus.Confirmed;
                    subOrder.ConfirmedAt = now;
                    subOrder.LastUpdatedAt = now;
                }

                _logger.LogInformation("Order {OrderNumber} confirmed with {SubOrderCount} sub-orders", order.OrderNumber, order.SellerSubOrders.Count);
            }
            else
            {
                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = now;

                // Update all seller sub-orders to Cancelled status
                foreach (var subOrder in order.SellerSubOrders)
                {
                    subOrder.Status = SellerSubOrderStatus.Cancelled;
                    subOrder.CancelledAt = now;
                    subOrder.LastUpdatedAt = now;
                }

                _logger.LogInformation("Order {OrderNumber} cancelled due to payment failure", order.OrderNumber);
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

            var now = DateTimeOffset.UtcNow;
            subOrder.Status = command.NewStatus;
            subOrder.LastUpdatedAt = now;

            // Set status-specific timestamps and properties
            switch (command.NewStatus)
            {
                case SellerSubOrderStatus.Processing:
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
            }

            await _sellerSubOrderRepository.UpdateAsync(subOrder);

            _logger.LogInformation(
                "Updated seller sub-order {SubOrderNumber} status to {Status}",
                subOrder.SubOrderNumber, command.NewStatus);

            return UpdateSellerSubOrderStatusResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating seller sub-order status for {SubOrderId}", subOrderId);
            return UpdateSellerSubOrderStatusResult.Failure("An error occurred while updating the sub-order status.");
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

        // Define valid status transitions
        var validTransitions = new Dictionary<SellerSubOrderStatus, SellerSubOrderStatus[]>
        {
            { SellerSubOrderStatus.Pending, [SellerSubOrderStatus.Confirmed, SellerSubOrderStatus.Cancelled] },
            { SellerSubOrderStatus.Confirmed, [SellerSubOrderStatus.Processing, SellerSubOrderStatus.Cancelled] },
            { SellerSubOrderStatus.Processing, [SellerSubOrderStatus.Shipped, SellerSubOrderStatus.Cancelled] },
            { SellerSubOrderStatus.Shipped, [SellerSubOrderStatus.Delivered] },
            { SellerSubOrderStatus.Delivered, [] },
            { SellerSubOrderStatus.Cancelled, [] },
            { SellerSubOrderStatus.Refunded, [] }
        };

        if (!validTransitions.TryGetValue(currentStatus, out var allowedStatuses) ||
            !allowedStatuses.Contains(newStatus))
        {
            errors.Add($"Cannot transition from {currentStatus} to {newStatus}.");
        }

        return errors;
    }
}
