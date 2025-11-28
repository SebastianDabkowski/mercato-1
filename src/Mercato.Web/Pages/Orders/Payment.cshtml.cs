using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for the payment method selection step.
/// </summary>
[Authorize(Roles = "Buyer")]
public class PaymentModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly ICheckoutValidationService _checkoutValidationService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentModel"/> class.
    /// </summary>
    /// <param name="cartService">The cart service.</param>
    /// <param name="checkoutValidationService">The checkout validation service.</param>
    /// <param name="paymentService">The payment service.</param>
    /// <param name="logger">The logger.</param>
    public PaymentModel(
        ICartService cartService,
        ICheckoutValidationService checkoutValidationService,
        IPaymentService paymentService,
        ILogger<PaymentModel> logger)
    {
        _cartService = cartService;
        _checkoutValidationService = checkoutValidationService;
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the cart result containing cart items.
    /// </summary>
    public GetCartResult CartResult { get; private set; } = null!;

    /// <summary>
    /// Gets the available payment methods.
    /// </summary>
    public IReadOnlyList<PaymentMethod> PaymentMethods { get; private set; } = [];

    /// <summary>
    /// Gets or sets the selected payment method ID.
    /// </summary>
    [BindProperty]
    public string SelectedPaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the cart is empty.
    /// </summary>
    public bool IsCartEmpty => CartResult?.ItemsByStore == null || CartResult.ItemsByStore.Count == 0;

    /// <summary>
    /// Gets the delivery address from session.
    /// </summary>
    public CheckoutAddressData? DeliveryAddress { get; private set; }

    /// <summary>
    /// Gets the shipping data from session.
    /// </summary>
    public CheckoutShippingData? ShippingData { get; private set; }

    /// <summary>
    /// Gets the total order amount.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Gets the stock validation issues if any.
    /// </summary>
    public IReadOnlyList<StockValidationIssue> StockIssues { get; private set; } = [];

    /// <summary>
    /// Gets the price change issues if any.
    /// </summary>
    public IReadOnlyList<PriceChangeIssue> PriceChanges { get; private set; } = [];

    /// <summary>
    /// Gets a value indicating whether there are validation issues.
    /// </summary>
    public bool HasValidationIssues => StockIssues.Count > 0 || PriceChanges.Count > 0;

    /// <summary>
    /// Handles GET requests for the payment page.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Check for address data from previous step
        if (!TryLoadDeliveryAddress())
        {
            TempData["Error"] = "Please select a delivery address first.";
            return RedirectToPage("Checkout");
        }

        // Check for shipping data from previous step
        if (!TryLoadShippingData())
        {
            TempData["Error"] = "Please select shipping methods first.";
            return RedirectToPage("Shipping");
        }

        // Load cart
        CartResult = await _cartService.GetCartAsync(new GetCartQuery { BuyerId = buyerId });

        if (!CartResult.Succeeded)
        {
            ErrorMessage = string.Join(", ", CartResult.Errors);
            return Page();
        }

        if (IsCartEmpty)
        {
            return RedirectToPage("/Cart/Index");
        }

        // Calculate total
        TotalAmount = CartResult.TotalPrice + (ShippingData?.TotalShippingCost ?? 0);

        // Get available payment methods
        var paymentResult = await _paymentService.GetPaymentMethodsAsync();

        if (!paymentResult.Succeeded)
        {
            ErrorMessage = string.Join(", ", paymentResult.Errors);
            return Page();
        }

        PaymentMethods = paymentResult.Methods.Where(m => m.IsEnabled).ToList();

        // Pre-select default method
        var defaultMethod = PaymentMethods.FirstOrDefault(m => m.IsDefault) ?? PaymentMethods.FirstOrDefault();
        if (defaultMethod != null)
        {
            SelectedPaymentMethodId = defaultMethod.Id;
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to proceed with payment.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Check for address data from previous step
        if (!TryLoadDeliveryAddress())
        {
            TempData["Error"] = "Please select a delivery address first.";
            return RedirectToPage("Checkout");
        }

        // Check for shipping data from previous step
        if (!TryLoadShippingData())
        {
            TempData["Error"] = "Please select shipping methods first.";
            return RedirectToPage("Shipping");
        }

        // Load cart
        CartResult = await _cartService.GetCartAsync(new GetCartQuery { BuyerId = buyerId });

        if (!CartResult.Succeeded || IsCartEmpty)
        {
            return RedirectToPage("/Cart/Index");
        }

        // Get payment methods for display if validation fails
        var paymentResult = await _paymentService.GetPaymentMethodsAsync();
        if (paymentResult.Succeeded)
        {
            PaymentMethods = paymentResult.Methods.Where(m => m.IsEnabled).ToList();
        }

        // Calculate total
        TotalAmount = CartResult.TotalPrice + (ShippingData?.TotalShippingCost ?? 0);

        // Validate payment method selection
        if (string.IsNullOrEmpty(SelectedPaymentMethodId))
        {
            ModelState.AddModelError(string.Empty, "Please select a payment method.");
            return Page();
        }

        // Validate stock and prices before placing order
        var validationResult = await _checkoutValidationService.ValidateCheckoutAsync(
            new ValidateCheckoutCommand { BuyerId = buyerId });

        if (!validationResult.Succeeded)
        {
            if (validationResult.HasStockIssues || validationResult.HasPriceChanges)
            {
                StockIssues = validationResult.StockIssues;
                PriceChanges = validationResult.PriceChanges;

                _logger.LogInformation(
                    "Checkout validation failed for buyer {BuyerId}: {StockIssueCount} stock issues, {PriceChangeCount} price changes",
                    buyerId, StockIssues.Count, PriceChanges.Count);

                // Keep checkout data for redisplay
                TempData.Keep("CheckoutAddress");
                TempData.Keep("CheckoutShipping");

                return Page();
            }

            ErrorMessage = string.Join(", ", validationResult.Errors);
            TempData.Keep("CheckoutAddress");
            TempData.Keep("CheckoutShipping");
            return Page();
        }

        // Store validated items in TempData for order creation after payment
        StoreValidatedItemsInTempData(validationResult.ValidatedItems);

        // Get the callback URL
        var callbackUrl = Url.Page(
            "PaymentCallback",
            pageHandler: null,
            values: null,
            protocol: Request.Scheme);

        // Initiate payment
        var initiateResult = await _paymentService.InitiatePaymentAsync(new InitiatePaymentCommand
        {
            BuyerId = buyerId,
            PaymentMethodId = SelectedPaymentMethodId,
            Amount = TotalAmount,
            ReturnUrl = callbackUrl ?? "",
            CancelUrl = Url.Page("Payment", pageHandler: null, values: null, protocol: Request.Scheme) ?? ""
        });

        if (!initiateResult.Succeeded)
        {
            if (initiateResult.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", initiateResult.Errors);
            return Page();
        }

        // Store payment transaction ID for callback
        TempData["PaymentTransactionId"] = initiateResult.TransactionId.ToString();

        // Keep checkout data
        TempData.Keep("CheckoutAddress");
        TempData.Keep("CheckoutShipping");
        TempData.Keep("ValidatedItems");

        _logger.LogInformation(
            "Payment initiated for buyer {BuyerId}, transaction {TransactionId}",
            buyerId, initiateResult.TransactionId);

        // Redirect to payment provider (simulated)
        if (initiateResult.RequiresRedirect && !string.IsNullOrEmpty(initiateResult.RedirectUrl))
        {
            return Redirect(initiateResult.RedirectUrl);
        }

        // If no redirect needed, go directly to callback (the callback handler determines success)
        return RedirectToPage("PaymentCallback", new { transactionId = initiateResult.TransactionId });
    }

    /// <summary>
    /// Handles POST requests to accept price changes and update cart.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAcceptPriceChangesAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        // Update cart prices to current
        await _checkoutValidationService.UpdateCartPricesToCurrentAsync(buyerId);

        TempData["Success"] = "Cart prices have been updated. Please review and continue.";
        TempData.Keep("CheckoutAddress");
        TempData.Keep("CheckoutShipping");

        return RedirectToPage();
    }

    private void StoreValidatedItemsInTempData(IReadOnlyList<ValidatedCartItem> items)
    {
        var itemsData = items.Select(i => new
        {
            i.CartItemId,
            i.ProductId,
            i.StoreId,
            i.ProductTitle,
            i.UnitPrice,
            i.Quantity,
            i.StoreName
        }).ToList();

        TempData["ValidatedItems"] = JsonSerializer.Serialize(itemsData);
    }

    private bool TryLoadDeliveryAddress()
    {
        if (TempData.Peek("CheckoutAddress") is string addressJson)
        {
            try
            {
                DeliveryAddress = JsonSerializer.Deserialize<CheckoutAddressData>(addressJson);
                return DeliveryAddress != null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize checkout address");
            }
        }

        return false;
    }

    private bool TryLoadShippingData()
    {
        if (TempData.Peek("CheckoutShipping") is string shippingJson)
        {
            try
            {
                ShippingData = JsonSerializer.Deserialize<CheckoutShippingData>(shippingJson);
                return ShippingData != null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize checkout shipping");
            }
        }

        return false;
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
