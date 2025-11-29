using Mercato.Orders.Application.Queries;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Admin.Orders;

/// <summary>
/// Page model for the admin order details page with full shipping status history.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IOrderService orderService,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
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
    /// Gets the shipping status histories for each sub-order.
    /// Key is seller sub-order ID.
    /// </summary>
    public Dictionary<Guid, IReadOnlyList<ShippingStatusHistory>> SubOrderStatusHistories { get; private set; } = new();

    /// <summary>
    /// Handles GET requests for the admin order details page.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToPage("Index");
        }

        try
        {
            // Admin can view any order - use a special method that doesn't require buyer ID
            var result = await _orderService.GetAdminOrdersAsync(new AdminOrderFilterQuery
            {
                SearchTerm = id.Value.ToString(),
                Page = 1,
                PageSize = 1
            });

            if (!result.Succeeded || result.Orders.Count == 0)
            {
                // Try to find by order ID directly using a broader search
                // For now, show not found
                ErrorMessage = "Order not found or an error occurred.";
                return Page();
            }

            // Get the first matching order - should be exact match
            Order = result.Orders.FirstOrDefault(o => o.Id == id.Value);
            if (Order == null)
            {
                ErrorMessage = "Order not found.";
                return Page();
            }

            // Load shipping status history for each sub-order
            foreach (var subOrder in Order.SellerSubOrders)
            {
                var historyResult = await _orderService.GetShippingStatusHistoryAsync(subOrder.Id);
                if (historyResult.Succeeded)
                {
                    SubOrderStatusHistories[subOrder.Id] = historyResult.History;
                }
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin order details for {OrderId}", id);
            ErrorMessage = "An error occurred while loading the order details.";
            return Page();
        }
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
}
