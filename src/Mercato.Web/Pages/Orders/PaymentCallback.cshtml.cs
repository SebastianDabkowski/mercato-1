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
    private readonly ILogger<PaymentCallbackModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentCallbackModel"/> class.
    /// </summary>
    /// <param name="paymentService">The payment service.</param>
    /// <param name="logger">The logger.</param>
    public PaymentCallbackModel(
        IPaymentService paymentService,
        ILogger<PaymentCallbackModel> logger)
    {
        _paymentService = paymentService;
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
        IsPaymentSuccessful = Transaction?.Status == PaymentStatus.Completed;

        if (IsPaymentSuccessful)
        {
            _logger.LogInformation(
                "Payment completed successfully for buyer {BuyerId}, transaction {TransactionId}",
                buyerId, transactionId);

            // Store order confirmation data
            StoreOrderConfirmationData();

            // Clear checkout data
            TempData.Remove("CheckoutAddress");
            TempData.Remove("CheckoutShipping");
            TempData.Remove("PaymentTransactionId");

            return RedirectToPage("Confirmation", new { transactionId = Transaction?.Id });
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

    private void StoreOrderConfirmationData()
    {
        if (Transaction == null) return;

        var confirmationData = new OrderConfirmationData
        {
            TransactionId = Transaction.Id,
            OrderNumber = $"ORD-{Transaction.Id.ToString("N")[..8].ToUpper()}",
            Amount = Transaction.Amount,
            PaymentMethod = Transaction.PaymentMethodId,
            CompletedAt = Transaction.CompletedAt ?? DateTimeOffset.UtcNow,
            DeliveryAddress = DeliveryAddress,
            ShippingData = ShippingData
        };

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
/// Data class for order confirmation stored in TempData.
/// </summary>
public class OrderConfirmationData
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    public decimal Amount { get; set; }

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
}
