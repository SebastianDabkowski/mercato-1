using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for handling payment provider callbacks.
/// </summary>
[Authorize(Roles = "Buyer")]
public class PaymentCallbackModel : PageModel
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<PaymentCallbackModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentCallbackModel"/> class.
    /// </summary>
    /// <param name="paymentService">The payment service.</param>
    /// <param name="orderService">The order service.</param>
    /// <param name="cartRepository">The cart repository.</param>
    /// <param name="logger">The logger.</param>
    public PaymentCallbackModel(
        IPaymentService paymentService,
        IOrderService orderService,
        ICartRepository cartRepository,
        ILogger<PaymentCallbackModel> logger)
    {
        _paymentService = paymentService;
        _orderService = orderService;
        _cartRepository = cartRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets the payment transaction.
    /// </summary>
    public PaymentTransaction? Transaction { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the payment was successful.
    /// </summary>
    public bool IsPaymentSuccessful { get; private set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the delivery address from session.
    /// </summary>
    public CheckoutAddressData? DeliveryAddress { get; private set; }

    /// <summary>
    /// Gets the shipping data from session.
    /// </summary>
    public CheckoutShippingData? ShippingData { get; private set; }

    /// <summary>
    /// Handles GET requests for the payment callback.
    /// </summary>
    /// <param name="transactionId">The transaction ID from the payment provider.</param>
    /// <param name="success">Whether the payment was successful.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? transactionId, bool success = false)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Try to get transaction ID from query or TempData
        if (!transactionId.HasValue || transactionId.Value == Guid.Empty)
        {
            if (TempData["PaymentTransactionId"] is string storedId && Guid.TryParse(storedId, out var parsedId))
            {
                transactionId = parsedId;
            }
            else
            {
                TempData["Error"] = "Invalid payment callback. Transaction not found.";
                return RedirectToPage("Payment");
            }
        }

        // Load checkout data
        TryLoadDeliveryAddress();
        TryLoadShippingData();

        // Process the payment callback
        var callbackResult = await _paymentService.ProcessPaymentCallbackAsync(new ProcessPaymentCallbackCommand
        {
            TransactionId = transactionId.Value,
            BuyerId = buyerId,
            IsSuccess = success,
            ExternalReferenceId = null
        });

        if (!callbackResult.Succeeded)
        {
            if (callbackResult.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", callbackResult.Errors);
            return Page();
        }

        Transaction = callbackResult.Transaction;
        IsPaymentSuccessful = Transaction?.Status == PaymentStatus.Paid;

        if (IsPaymentSuccessful)
        {
            _logger.LogInformation(
                "Payment completed successfully for buyer {BuyerId}, transaction {TransactionId}",
                buyerId, transactionId);

            // Create the order with validated items
            var orderResult = await CreateOrderAsync(buyerId, transactionId.Value);

            if (!orderResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to create order for buyer {BuyerId}, transaction {TransactionId}: {Errors}",
                    buyerId, transactionId, string.Join(", ", orderResult.Errors));

                ErrorMessage = "Payment was successful but order creation failed. Please contact support.";
                return Page();
            }

            // Clear the buyer's cart after successful order creation
            await ClearCartAsync(buyerId);

            // Load items for confirmation data
            var validatedItems = LoadValidatedItems();

            // Store order confirmation data
            await StoreOrderConfirmationDataAsync(
                buyerId,
                orderResult.OrderNumber ?? "",
                orderResult.OrderId ?? Guid.Empty,
                validatedItems);

            // Clear checkout data
            TempData.Remove("CheckoutAddress");
            TempData.Remove("CheckoutShipping");
            TempData.Remove("PaymentTransactionId");
            TempData.Remove("ValidatedItems");

            return RedirectToPage("Confirmation", new { orderId = orderResult.OrderId });
        }
        else
        {
            _logger.LogWarning(
                "Payment failed for buyer {BuyerId}, transaction {TransactionId}",
                buyerId, transactionId);

            ErrorMessage = Transaction?.ErrorMessage ?? "Payment was not completed. Please try again.";
            return Page();
        }
    }

    private async Task<CreateOrderResult> CreateOrderAsync(string buyerId, Guid transactionId)
    {
        // Load validated items from TempData
        var items = LoadValidatedItems();
        if (items == null || items.Count == 0)
        {
            return CreateOrderResult.Failure("No validated items found. Please try again.");
        }

        // Set shipping method names on items based on store
        if (ShippingData?.ShippingMethodNames != null)
        {
            foreach (var item in items)
            {
                if (ShippingData.ShippingMethodNames.TryGetValue(item.StoreId, out var methodName))
                {
                    item.ShippingMethodName = methodName;
                }
            }
        }

        var deliveryAddress = new DeliveryAddressInfo();
        if (DeliveryAddress != null)
        {
            deliveryAddress.FullName = DeliveryAddress.FullName;
            deliveryAddress.AddressLine1 = DeliveryAddress.AddressLine1;
            deliveryAddress.AddressLine2 = DeliveryAddress.AddressLine2;
            deliveryAddress.City = DeliveryAddress.City;
            deliveryAddress.State = DeliveryAddress.State;
            deliveryAddress.PostalCode = DeliveryAddress.PostalCode;
            deliveryAddress.Country = DeliveryAddress.Country;
            deliveryAddress.PhoneNumber = DeliveryAddress.PhoneNumber;
        }

        var command = new CreateOrderCommand
        {
            BuyerId = buyerId,
            PaymentTransactionId = transactionId,
            Items = items,
            ShippingTotal = ShippingData?.TotalShippingCost ?? 0,
            DeliveryAddress = deliveryAddress,
            PaymentMethodName = Transaction != null ? GetPaymentMethodDisplayName(Transaction.PaymentMethodId) : null
        };

        return await _orderService.CreateOrderAsync(command);
    }

    /// <summary>
    /// Gets the display name for a payment method ID.
    /// </summary>
    /// <param name="paymentMethodId">The payment method ID.</param>
    /// <returns>The display name for the payment method.</returns>
    private static string GetPaymentMethodDisplayName(string paymentMethodId)
    {
        return paymentMethodId switch
        {
            "credit_card" => "Credit Card",
            "paypal" => "PayPal",
            _ => paymentMethodId
        };
    }

    private List<CreateOrderItem>? LoadValidatedItems()
    {
        if (TempData.Peek("ValidatedItems") is not string itemsJson)
        {
            // Fallback: get items from cart (this path should not normally happen)
            return null;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<ValidatedItemData>>(itemsJson);
            if (items == null)
            {
                return null;
            }

            return items.Select(i => new CreateOrderItem
            {
                ProductId = i.ProductId,
                StoreId = i.StoreId,
                ProductTitle = i.ProductTitle,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                StoreName = i.StoreName,
                ProductImageUrl = null
            }).ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize validated items");
            return null;
        }
    }

    private async Task ClearCartAsync(string buyerId)
    {
        try
        {
            var cart = await _cartRepository.GetByBuyerIdAsync(buyerId);
            if (cart != null)
            {
                await _cartRepository.DeleteAsync(cart);
                _logger.LogInformation("Cleared cart for buyer {BuyerId} after order creation", buyerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cart for buyer {BuyerId}", buyerId);
        }
    }

    private async Task StoreOrderConfirmationDataAsync(
        string buyerId,
        string orderNumber,
        Guid orderId,
        List<CreateOrderItem>? validatedItems)
    {
        if (Transaction == null) return;

        // Calculate items subtotal
        var itemsSubtotal = validatedItems?.Sum(i => i.UnitPrice * i.Quantity) ?? 0;

        var confirmationData = new OrderConfirmationData
        {
            TransactionId = Transaction.Id,
            OrderId = orderId,
            OrderNumber = orderNumber,
            Amount = Transaction.Amount,
            ItemsSubtotal = itemsSubtotal,
            PaymentMethod = Transaction.PaymentMethodId,
            CompletedAt = Transaction.CompletedAt ?? Transaction.CreatedAt,
            DeliveryAddress = DeliveryAddress,
            ShippingData = ShippingData,
            Items = validatedItems?.Select(i => new OrderItemData
            {
                ProductId = i.ProductId,
                ProductTitle = i.ProductTitle,
                StoreName = i.StoreName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList() ?? []
        };

        // Send confirmation email
        var buyerEmail = User.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrEmpty(buyerEmail))
        {
            var emailResult = await _orderService.SendOrderConfirmationEmailAsync(orderId, buyerEmail);
            confirmationData.EmailSent = emailResult.Succeeded;

            if (!emailResult.Succeeded)
            {
                _logger.LogWarning(
                    "Failed to send confirmation email for order {OrderNumber}: {Errors}",
                    orderNumber, string.Join(", ", emailResult.Errors));
            }
        }
        else
        {
            _logger.LogWarning("No email address found for buyer {BuyerId}, skipping confirmation email", buyerId);
        }

        TempData["OrderConfirmation"] = JsonSerializer.Serialize(confirmationData);
    }

    private void TryLoadDeliveryAddress()
    {
        if (TempData.Peek("CheckoutAddress") is string addressJson)
        {
            try
            {
                DeliveryAddress = JsonSerializer.Deserialize<CheckoutAddressData>(addressJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize checkout address");
            }
        }
    }

    private void TryLoadShippingData()
    {
        if (TempData.Peek("CheckoutShipping") is string shippingJson)
        {
            try
            {
                ShippingData = JsonSerializer.Deserialize<CheckoutShippingData>(shippingJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize checkout shipping");
            }
        }
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

/// <summary>
/// Data class for validated cart item stored in TempData.
/// </summary>
public class ValidatedItemData
{
    /// <summary>
    /// Gets or sets the cart item ID.
    /// </summary>
    public Guid CartItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;
}

/// <summary>
/// Data class for order confirmation stored in TempData.
/// </summary>
public class OrderConfirmationData
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the items subtotal.
    /// </summary>
    public decimal ItemsSubtotal { get; set; }

    /// <summary>
    /// Gets or sets the payment method used.
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the completion date.
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery address.
    /// </summary>
    public CheckoutAddressData? DeliveryAddress { get; set; }

    /// <summary>
    /// Gets or sets the shipping data.
    /// </summary>
    public CheckoutShippingData? ShippingData { get; set; }

    /// <summary>
    /// Gets or sets the ordered items.
    /// </summary>
    public List<OrderItemData> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether a confirmation email was sent.
    /// </summary>
    public bool EmailSent { get; set; }
}

/// <summary>
/// Data class for an ordered item stored in TempData.
/// </summary>
public class OrderItemData
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets the total price for this item.
    /// </summary>
    public decimal TotalPrice => UnitPrice * Quantity;
}
