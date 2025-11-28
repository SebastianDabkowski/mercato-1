using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Orders;

/// <summary>
/// Page model for the seller orders index page.
/// </summary>
[Authorize(Roles = "Seller")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IOrderService orderService,
        IStoreProfileService storeProfileService,
        ILogger<IndexModel> logger)
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
    /// Gets the list of seller sub-orders.
    /// </summary>
    public IReadOnlyList<SellerSubOrder> SubOrders { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Handles GET requests for the seller orders index page.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
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
                return Page();
            }

            var result = await _orderService.GetSellerSubOrdersAsync(Store.Id);
            if (!result.Succeeded)
            {
                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            SubOrders = result.SellerSubOrders;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading orders for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your orders.";
            return Page();
        }
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
