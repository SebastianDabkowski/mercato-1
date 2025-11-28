using Mercato.Orders.Application.Services;
using Mercato.Payments.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Mercato.Orders.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for the order confirmation page.
/// </summary>
[Authorize(Roles = "Buyer")]
public class ConfirmationModel : PageModel
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<ConfirmationModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationModel"/> class.
    /// </summary>
    /// <param name="paymentService">The payment service.</param>
    /// <param name="orderService">The order service.</param>
    /// <param name="emailSettings">The email settings.</param>
    /// <param name="logger">The logger.</param>
    public ConfirmationModel(
        IPaymentService paymentService,
        IOrderService orderService,
        IOptions<EmailSettings> emailSettings,
        ILogger<ConfirmationModel> logger)
    {
        _paymentService = paymentService;
        _orderService = orderService;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the order confirmation data.
    /// </summary>
    public OrderConfirmationData? ConfirmationData { get; private set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the estimated delivery days message.
    /// </summary>
    public string EstimatedDeliveryDays => _emailSettings.EstimatedDeliveryDays;

    /// <summary>
    /// Handles GET requests for the confirmation page.
    /// </summary>
    /// <param name="orderId">The order ID (for access via order history).</param>
    /// <param name="transactionId">The transaction ID (legacy parameter).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? orderId, Guid? transactionId)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Try to get confirmation from TempData first (immediate redirect after payment)
        if (TryLoadConfirmationData())
        {
            return Page();
        }

        // If orderId is provided, load from order (for access via order history)
        if (orderId.HasValue && orderId.Value != Guid.Empty)
        {
            return await LoadFromOrderAsync(orderId.Value, buyerId);
        }

        // If no TempData and no orderId, try to fetch from transaction ID (legacy fallback)
        if (transactionId.HasValue && transactionId.Value != Guid.Empty)
        {
            return await LoadFromTransactionAsync(transactionId.Value, buyerId);
        }

        TempData["Error"] = "Order confirmation not found.";
        return RedirectToPage("Index");
    }

    private async Task<IActionResult> LoadFromOrderAsync(Guid orderId, string buyerId)
    {
        var orderResult = await _orderService.GetOrderAsync(orderId, buyerId);

        if (!orderResult.Succeeded)
        {
            if (orderResult.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", orderResult.Errors);
            return Page();
        }

        if (orderResult.Order == null)
        {
            ErrorMessage = "Order not found.";
            return Page();
        }

        var order = orderResult.Order;

        // Try to get payment method from transaction
        string paymentMethod = "credit_card"; // Default value
        if (order.PaymentTransactionId.HasValue && order.PaymentTransactionId.Value != Guid.Empty)
        {
            var transactionResult = await _paymentService.GetTransactionAsync(order.PaymentTransactionId.Value, buyerId);
            if (transactionResult.Succeeded && transactionResult.Transaction != null)
            {
                paymentMethod = transactionResult.Transaction.PaymentMethodId;
            }
        }

        // Build confirmation data from order
        ConfirmationData = new OrderConfirmationData
        {
            TransactionId = order.PaymentTransactionId ?? Guid.Empty,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Amount = order.TotalAmount,
            ItemsSubtotal = order.ItemsSubtotal,
            PaymentMethod = paymentMethod,
            CompletedAt = order.ConfirmedAt ?? order.CreatedAt,
            DeliveryAddress = new CheckoutAddressData
            {
                FullName = order.DeliveryFullName,
                AddressLine1 = order.DeliveryAddressLine1,
                AddressLine2 = order.DeliveryAddressLine2,
                City = order.DeliveryCity,
                State = order.DeliveryState,
                PostalCode = order.DeliveryPostalCode,
                Country = order.DeliveryCountry,
                PhoneNumber = order.DeliveryPhoneNumber
            },
            ShippingData = new CheckoutShippingData
            {
                TotalShippingCost = order.ShippingTotal
            },
            Items = order.Items.Select(i => new OrderItemData
            {
                ProductId = i.ProductId,
                ProductTitle = i.ProductTitle,
                StoreName = i.StoreName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList(),
            EmailSent = true // Assume email was sent if loading from order history
        };

        return Page();
    }

    private async Task<IActionResult> LoadFromTransactionAsync(Guid transactionId, string buyerId)
    {
        var transactionResult = await _paymentService.GetTransactionAsync(transactionId, buyerId);

        if (!transactionResult.Succeeded)
        {
            if (transactionResult.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", transactionResult.Errors);
            return Page();
        }

        if (transactionResult.Transaction == null)
        {
            ErrorMessage = "Transaction not found.";
            return Page();
        }

        // Try to find the order by transaction ID
        var orderResult = await _orderService.GetOrderByTransactionAsync(transactionId, buyerId);
        if (orderResult.Succeeded && orderResult.Order != null)
        {
            return await LoadFromOrderAsync(orderResult.Order.Id, buyerId);
        }

        // Fallback: Build minimal confirmation data from transaction
        ConfirmationData = new OrderConfirmationData
        {
            TransactionId = transactionResult.Transaction.Id,
            OrderNumber = $"ORD-{transactionResult.Transaction.Id.ToString("N")[..8].ToUpper()}",
            Amount = transactionResult.Transaction.Amount,
            PaymentMethod = transactionResult.Transaction.PaymentMethodId,
            CompletedAt = transactionResult.Transaction.CompletedAt ?? transactionResult.Transaction.CreatedAt
        };

        return Page();
    }

    private bool TryLoadConfirmationData()
    {
        if (TempData["OrderConfirmation"] is string confirmationJson)
        {
            try
            {
                ConfirmationData = JsonSerializer.Deserialize<OrderConfirmationData>(confirmationJson);
                return ConfirmationData != null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize order confirmation");
            }
        }

        return false;
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
