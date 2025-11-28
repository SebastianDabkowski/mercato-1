using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for the order history page.
/// </summary>
[Authorize(Roles = "Buyer")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IOrderService orderService,
        ILogger<IndexModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of orders.
    /// </summary>
    public IReadOnlyList<Order> Orders { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Handles GET requests for the orders page.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        var result = await _orderService.GetOrdersForBuyerAsync(buyerId);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Orders = result.Orders;
        return Page();
    }

    /// <summary>
    /// Gets the CSS class for an order status badge.
    /// </summary>
    /// <param name="status">The order status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Confirmed => "bg-info",
        OrderStatus.Processing => "bg-primary",
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
        OrderStatus.Pending => "Pending",
        OrderStatus.Confirmed => "Confirmed",
        OrderStatus.Processing => "Processing",
        OrderStatus.Shipped => "Shipped",
        OrderStatus.Delivered => "Delivered",
        OrderStatus.Cancelled => "Cancelled",
        OrderStatus.Refunded => "Refunded",
        _ => status.ToString()
    };

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
