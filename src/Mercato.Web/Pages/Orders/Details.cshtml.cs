using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Mercato.Orders.Infrastructure;
using System.Security.Claims;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for the order details page.
/// </summary>
[Authorize(Roles = "Buyer")]
public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly EmailSettings _emailSettings;
    private readonly ReturnSettings _returnSettings;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="paymentService">The payment service.</param>
    /// <param name="emailSettings">The email settings.</param>
    /// <param name="returnSettings">The return settings.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IOrderService orderService,
        IPaymentService paymentService,
        IOptions<EmailSettings> emailSettings,
        IOptions<ReturnSettings> returnSettings,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _emailSettings = emailSettings.Value;
        _returnSettings = returnSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the order.
    /// </summary>
    public Order? Order { get; private set; }

    /// <summary>
    /// Gets the payment transaction for the order.
    /// </summary>
    public PaymentTransaction? PaymentTransaction { get; private set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the estimated delivery days message.
    /// </summary>
    public string EstimatedDeliveryDays => _emailSettings.EstimatedDeliveryDays;

    /// <summary>
    /// Gets the return window days.
    /// </summary>
    public int ReturnWindowDays => _returnSettings.ReturnWindowDays;

    /// <summary>
    /// Gets the return eligibility information for each sub-order.
    /// Key is sub-order ID, value indicates if return can be initiated (true) or has existing return request (null for existing).
    /// </summary>
    public Dictionary<Guid, ReturnEligibility> SubOrderReturnEligibility { get; private set; } = new();

    /// <summary>
    /// Gets the return requests for sub-orders that have one.
    /// Key is sub-order ID.
    /// </summary>
    public Dictionary<Guid, ReturnRequest> SubOrderReturnRequests { get; private set; } = new();

    /// <summary>
    /// Handles GET requests for the order details page.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        if (!id.HasValue || id.Value == Guid.Empty)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToPage("Index");
        }

        var result = await _orderService.GetOrderAsync(id.Value, buyerId);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Order = result.Order;

        // Load payment transaction if available
        if (Order?.PaymentTransactionId.HasValue == true)
        {
            var paymentResult = await _paymentService.GetTransactionAsync(Order.PaymentTransactionId.Value, buyerId);
            if (paymentResult.Succeeded)
            {
                PaymentTransaction = paymentResult.Transaction;
            }
        }

        // Load return eligibility and existing return requests for each sub-order
        if (Order != null)
        {
            foreach (var subOrder in Order.SellerSubOrders)
            {
                var canInitiateResult = await _orderService.CanInitiateReturnAsync(subOrder.Id, buyerId);
                if (canInitiateResult.Succeeded)
                {
                    SubOrderReturnEligibility[subOrder.Id] = new ReturnEligibility
                    {
                        CanInitiate = canInitiateResult.CanInitiate,
                        BlockedReason = canInitiateResult.BlockedReason
                    };
                }
            }

            // Load all return requests for this buyer and match to sub-orders
            var returnRequestsResult = await _orderService.GetReturnRequestsForBuyerAsync(buyerId);
            if (returnRequestsResult.Succeeded)
            {
                foreach (var returnRequest in returnRequestsResult.ReturnRequests)
                {
                    if (Order.SellerSubOrders.Any(s => s.Id == returnRequest.SellerSubOrderId))
                    {
                        SubOrderReturnRequests[returnRequest.SellerSubOrderId] = returnRequest;
                    }
                }
            }
        }

        return Page();
    }

    /// <summary>
    /// Gets the CSS class for an order status badge.
    /// </summary>
    /// <param name="status">The order status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.New => "bg-warning text-dark",
        OrderStatus.Paid => "bg-info",
        OrderStatus.Preparing => "bg-primary",
        OrderStatus.Shipped => "bg-secondary",
        OrderStatus.Delivered => "bg-success",
        OrderStatus.Cancelled => "bg-danger",
        OrderStatus.Refunded => "bg-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for an order status.
    /// </summary>
    /// <param name="status">The order status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(OrderStatus status) => status switch
    {
        OrderStatus.New => "New",
        OrderStatus.Paid => "Paid",
        OrderStatus.Preparing => "Preparing",
        OrderStatus.Shipped => "Shipped",
        OrderStatus.Delivered => "Delivered",
        OrderStatus.Cancelled => "Cancelled",
        OrderStatus.Refunded => "Refunded",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the CSS class for a seller sub-order status badge.
    /// </summary>
    /// <param name="status">The seller sub-order status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetSubOrderStatusBadgeClass(SellerSubOrderStatus status) => status switch
    {
        SellerSubOrderStatus.New => "bg-warning text-dark",
        SellerSubOrderStatus.Paid => "bg-info",
        SellerSubOrderStatus.Preparing => "bg-primary",
        SellerSubOrderStatus.Shipped => "bg-secondary",
        SellerSubOrderStatus.Delivered => "bg-success",
        SellerSubOrderStatus.Cancelled => "bg-danger",
        SellerSubOrderStatus.Refunded => "bg-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a seller sub-order status.
    /// </summary>
    /// <param name="status">The seller sub-order status.</param>
    /// <returns>The display text.</returns>
    public static string GetSubOrderStatusDisplayText(SellerSubOrderStatus status) => status switch
    {
        SellerSubOrderStatus.New => "New",
        SellerSubOrderStatus.Paid => "Paid",
        SellerSubOrderStatus.Preparing => "Preparing",
        SellerSubOrderStatus.Shipped => "Shipped",
        SellerSubOrderStatus.Delivered => "Delivered",
        SellerSubOrderStatus.Cancelled => "Cancelled",
        SellerSubOrderStatus.Refunded => "Refunded",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the CSS class for a return status badge.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetReturnStatusBadgeClass(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "bg-warning text-dark",
        ReturnStatus.UnderReview => "bg-info",
        ReturnStatus.Approved => "bg-success",
        ReturnStatus.Rejected => "bg-danger",
        ReturnStatus.Completed => "bg-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a return status.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The display text.</returns>
    public static string GetReturnStatusDisplayText(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "Requested",
        ReturnStatus.UnderReview => "Under Review",
        ReturnStatus.Approved => "Approved",
        ReturnStatus.Rejected => "Rejected",
        ReturnStatus.Completed => "Completed",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the CSS class for a seller sub-order item status badge.
    /// </summary>
    /// <param name="status">The item status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetItemStatusBadgeClass(SellerSubOrderItemStatus status) => status switch
    {
        SellerSubOrderItemStatus.New => "bg-secondary",
        SellerSubOrderItemStatus.Preparing => "bg-primary",
        SellerSubOrderItemStatus.Shipped => "bg-info",
        SellerSubOrderItemStatus.Delivered => "bg-success",
        SellerSubOrderItemStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a seller sub-order item status.
    /// </summary>
    /// <param name="status">The item status.</param>
    /// <returns>The display text.</returns>
    public static string GetItemStatusDisplayText(SellerSubOrderItemStatus status) => status switch
    {
        SellerSubOrderItemStatus.New => "New",
        SellerSubOrderItemStatus.Preparing => "Preparing",
        SellerSubOrderItemStatus.Shipped => "Shipped",
        SellerSubOrderItemStatus.Delivered => "Delivered",
        SellerSubOrderItemStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    /// <summary>
    /// Formats the shipping label with optional method name.
    /// </summary>
    /// <param name="shippingMethodName">The shipping method name, or null.</param>
    /// <returns>The formatted shipping label.</returns>
    public static string FormatShippingLabel(string? shippingMethodName)
    {
        if (string.IsNullOrEmpty(shippingMethodName))
        {
            return "Shipping:";
        }
        return $"Shipping ({shippingMethodName}):";
    }

    /// <summary>
    /// Gets the tracking URL for a shipping carrier and tracking number.
    /// </summary>
    /// <param name="carrier">The shipping carrier name.</param>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <returns>The tracking URL, or null if the carrier is not recognized.</returns>
    public static string? GetCarrierTrackingUrl(string? carrier, string? trackingNumber)
    {
        if (string.IsNullOrEmpty(carrier) || string.IsNullOrEmpty(trackingNumber))
        {
            return null;
        }

        var carrierLower = carrier.ToLowerInvariant().Trim();
        var encodedTrackingNumber = Uri.EscapeDataString(trackingNumber);

        return carrierLower switch
        {
            "ups" => $"https://www.ups.com/track?loc=en_US&tracknum={encodedTrackingNumber}",
            "fedex" => $"https://www.fedex.com/fedextrack/?tracknumbers={encodedTrackingNumber}",
            "usps" => $"https://tools.usps.com/go/TrackConfirmAction?tLabels={encodedTrackingNumber}",
            "dhl" => $"https://www.dhl.com/us-en/home/tracking/tracking-express.html?submit=1&tracking-id={encodedTrackingNumber}",
            _ => null
        };
    }

    /// <summary>
    /// Gets the CSS class for a payment status badge.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetPaymentStatusBadgeClass(PaymentStatus status) =>
        PaymentStatusDisplay.GetBadgeClass(status);

    /// <summary>
    /// Gets the display text for a payment status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>The display text.</returns>
    public static string GetPaymentStatusDisplayText(PaymentStatus status) =>
        PaymentStatusDisplay.GetDisplayText(status);

    /// <summary>
    /// Gets the icon class for a payment status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>The icon class.</returns>
    public static string GetPaymentStatusIconClass(PaymentStatus status) =>
        PaymentStatusDisplay.GetIconClass(status);

    /// <summary>
    /// Gets a buyer-friendly message for the payment status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <param name="refundedAmount">The refunded amount.</param>
    /// <returns>A buyer-friendly message.</returns>
    public static string GetPaymentStatusMessage(PaymentStatus status, decimal refundedAmount = 0) =>
        PaymentStatusDisplay.GetBuyerMessage(status, refundedAmount);

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

/// <summary>
/// Represents return eligibility information for a sub-order.
/// </summary>
public class ReturnEligibility
{
    /// <summary>
    /// Gets or sets whether a return can be initiated.
    /// </summary>
    public bool CanInitiate { get; set; }

    /// <summary>
    /// Gets or sets the reason why return cannot be initiated (if applicable).
    /// </summary>
    public string? BlockedReason { get; set; }
}
