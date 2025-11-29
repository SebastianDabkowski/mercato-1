using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
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
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="emailSettings">The email settings.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IOrderService orderService,
        IOptions<EmailSettings> emailSettings,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the order.
    /// </summary>
    public Order? Order { get; private set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the estimated delivery days message.
    /// </summary>
    public string EstimatedDeliveryDays => _emailSettings.EstimatedDeliveryDays;

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

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
