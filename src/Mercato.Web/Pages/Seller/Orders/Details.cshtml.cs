using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Orders;

/// <summary>
/// Page model for the seller order details page.
/// </summary>
[Authorize(Roles = "Seller")]
public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IOrderService orderService,
        IStoreProfileService storeProfileService,
        ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the seller's store.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets the seller sub-order.
    /// </summary>
    public SellerSubOrder? SubOrder { get; private set; }

    /// <summary>
    /// Gets the parent order.
    /// </summary>
    public Order? ParentOrder { get; private set; }

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the success message to display.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Gets whether the status can be updated.
    /// </summary>
    public bool CanUpdateStatus => SubOrder != null &&
        SubOrder.Status != SellerSubOrderStatus.Pending &&
        SubOrder.Status != SellerSubOrderStatus.Delivered &&
        SubOrder.Status != SellerSubOrderStatus.Cancelled &&
        SubOrder.Status != SellerSubOrderStatus.Refunded;

    /// <summary>
    /// Handles GET requests for the seller order details page.
    /// </summary>
    /// <param name="id">The seller sub-order ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        try
        {
            Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (Store == null)
            {
                ErrorMessage = "Store not found.";
                return Page();
            }

            var result = await _orderService.GetSellerSubOrderAsync(id, Store.Id);
            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }
                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            SubOrder = result.SellerSubOrder;
            ParentOrder = SubOrder?.Order;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading order details for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading the order details.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to update the order status.
    /// </summary>
    /// <param name="id">The seller sub-order ID.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="trackingNumber">The tracking number (optional).</param>
    /// <param name="shippingCarrier">The shipping carrier (optional).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync(
        Guid id,
        string newStatus,
        string? trackingNumber,
        string? shippingCarrier)
    {
        var sellerId = GetSellerId();
        if (string.IsNullOrEmpty(sellerId))
        {
            return Forbid();
        }

        try
        {
            Store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (Store == null)
            {
                ErrorMessage = "Store not found.";
                return Page();
            }

            if (!Enum.TryParse<SellerSubOrderStatus>(newStatus, out var status))
            {
                ErrorMessage = "Invalid status.";
                return await LoadSubOrderAndReturnPage(id);
            }

            var command = new UpdateSellerSubOrderStatusCommand
            {
                NewStatus = status,
                TrackingNumber = trackingNumber,
                ShippingCarrier = shippingCarrier
            };

            var result = await _orderService.UpdateSellerSubOrderStatusAsync(id, Store.Id, command);
            if (!result.Succeeded)
            {
                if (result.IsNotAuthorized)
                {
                    return Forbid();
                }
                ErrorMessage = string.Join(", ", result.Errors);
                return await LoadSubOrderAndReturnPage(id);
            }

            SuccessMessage = $"Order status updated to {GetStatusDisplayText(status)}.";
            return await LoadSubOrderAndReturnPage(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while updating the order status.";
            return await LoadSubOrderAndReturnPage(id);
        }
    }

    private async Task<IActionResult> LoadSubOrderAndReturnPage(Guid id)
    {
        if (Store != null)
        {
            var result = await _orderService.GetSellerSubOrderAsync(id, Store.Id);
            if (result.Succeeded)
            {
                SubOrder = result.SellerSubOrder;
                ParentOrder = SubOrder?.Order;
            }
        }
        return Page();
    }

    /// <summary>
    /// Gets the CSS class for a seller sub-order status badge.
    /// </summary>
    /// <param name="status">The seller sub-order status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(SellerSubOrderStatus status) => status switch
    {
        SellerSubOrderStatus.Pending => "bg-warning text-dark",
        SellerSubOrderStatus.Confirmed => "bg-info",
        SellerSubOrderStatus.Processing => "bg-primary",
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
    public static string GetStatusDisplayText(SellerSubOrderStatus status) => status switch
    {
        SellerSubOrderStatus.Pending => "Pending",
        SellerSubOrderStatus.Confirmed => "Confirmed",
        SellerSubOrderStatus.Processing => "Processing",
        SellerSubOrderStatus.Shipped => "Shipped",
        SellerSubOrderStatus.Delivered => "Delivered",
        SellerSubOrderStatus.Cancelled => "Cancelled",
        SellerSubOrderStatus.Refunded => "Refunded",
        _ => status.ToString()
    };

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
