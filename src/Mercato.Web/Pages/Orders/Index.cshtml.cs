using Mercato.Orders.Application.Queries;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Orders;

/// <summary>
/// Page model for the order history page with filtering support.
/// </summary>
[Authorize(Roles = "Buyer")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<IndexModel> _logger;
    private const int DefaultPageSize = 10;

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
    /// Gets the list of orders for the current page.
    /// </summary>
    public IReadOnlyList<Order> Orders { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total number of orders matching the filter.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int CurrentPage { get; private set; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; private set; } = DefaultPageSize;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Gets or sets the selected statuses for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public List<OrderStatus> Status { get; set; } = [];

    /// <summary>
    /// Gets or sets the start date for date range filter (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Gets or sets the store ID for seller filter (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid? StoreId { get; set; }

    /// <summary>
    /// Gets all available order statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<OrderStatus> AllStatuses => Enum.GetValues<OrderStatus>();

    /// <summary>
    /// Gets the distinct sellers from the current user's orders for the filter dropdown.
    /// </summary>
    public IReadOnlyList<(Guid StoreId, string StoreName)> AvailableSellers { get; private set; } = [];

    /// <summary>
    /// Handles GET requests for the orders page with filtering.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        var buyerId = GetBuyerId();
        if (string.IsNullOrEmpty(buyerId))
        {
            return Forbid();
        }

        CurrentPage = page < 1 ? 1 : page;

        var query = new BuyerOrderFilterQuery
        {
            BuyerId = buyerId,
            Statuses = Status,
            FromDate = FromDate,
            ToDate = ToDate,
            StoreId = StoreId,
            Page = CurrentPage,
            PageSize = DefaultPageSize
        };

        var result = await _orderService.GetFilteredOrdersForBuyerAsync(query);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Orders = result.Orders;
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
        PageSize = result.PageSize;

        // Load available sellers for the filter dropdown (from all buyer's orders)
        await LoadAvailableSellersAsync(buyerId);

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
    /// Gets the query string for pagination links that preserves filter state.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <returns>The query string.</returns>
    public string GetPaginationQueryString(int page)
    {
        var queryParams = new List<string> { $"page={page}" };

        foreach (var status in Status)
        {
            queryParams.Add($"Status={status}");
        }

        if (FromDate.HasValue)
        {
            queryParams.Add($"FromDate={FromDate.Value:yyyy-MM-dd}");
        }

        if (ToDate.HasValue)
        {
            queryParams.Add($"ToDate={ToDate.Value:yyyy-MM-dd}");
        }

        if (StoreId.HasValue)
        {
            queryParams.Add($"StoreId={StoreId.Value}");
        }

        return "?" + string.Join("&", queryParams);
    }

    private string? GetBuyerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task LoadAvailableSellersAsync(string buyerId)
    {
        // Get all orders (without filters) to build the list of available sellers
        var allOrdersResult = await _orderService.GetOrdersForBuyerAsync(buyerId);
        if (allOrdersResult.Succeeded)
        {
            var sellers = allOrdersResult.Orders
                .SelectMany(o => o.SellerSubOrders)
                .Select(s => (s.StoreId, s.StoreName))
                .Distinct()
                .OrderBy(s => s.StoreName)
                .ToList();

            AvailableSellers = sellers;
        }
    }
}
