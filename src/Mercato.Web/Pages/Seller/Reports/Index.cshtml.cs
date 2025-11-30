using Mercato.Orders.Application.Queries;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Reports;

/// <summary>
/// Page model for the seller revenue reports page with filtering and export support.
/// </summary>
[Authorize(Roles = "Seller")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<IndexModel> _logger;
    private const int DefaultPageSize = 10;

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
    /// Gets the list of report items for the current page.
    /// </summary>
    public IReadOnlyList<SellerReportItem> ReportItems { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total number of items matching the filter.
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
    /// Gets the total order value across all filtered items.
    /// </summary>
    public decimal TotalOrderValue { get; private set; }

    /// <summary>
    /// Gets the total commission amount across all filtered items.
    /// </summary>
    public decimal TotalCommissionAmount { get; private set; }

    /// <summary>
    /// Gets the total net amount across all filtered items.
    /// </summary>
    public decimal TotalNetAmount { get; private set; }

    /// <summary>
    /// Gets or sets the selected statuses for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public List<SellerSubOrderStatus> Status { get; set; } = [];

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
    /// Gets all available seller sub-order statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<SellerSubOrderStatus> AllStatuses => Enum.GetValues<SellerSubOrderStatus>();

    /// <summary>
    /// Handles GET requests for the seller reports page with filtering.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
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

            var query = new SellerReportFilterQuery
            {
                StoreId = Store.Id,
                Statuses = Status,
                FromDate = FromDate,
                ToDate = ToDate,
                Page = page,
                PageSize = DefaultPageSize
            };

            var result = await _orderService.GetSellerReportAsync(query);

            if (!result.Succeeded)
            {
                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            ReportItems = result.Items;
            TotalCount = result.TotalCount;
            CurrentPage = result.Page;
            TotalPages = result.TotalPages;
            PageSize = result.PageSize;
            TotalOrderValue = result.TotalOrderValue;
            TotalCommissionAmount = result.TotalCommissionAmount;
            TotalNetAmount = result.TotalNetAmount;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reports for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your reports.";
            return Page();
        }
    }

    /// <summary>
    /// Handles GET requests for exporting seller report to CSV.
    /// </summary>
    /// <returns>A file result containing the CSV data.</returns>
    public async Task<IActionResult> OnGetExportAsync()
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
                return NotFound("Store not found.");
            }

            var query = new SellerReportFilterQuery
            {
                StoreId = Store.Id,
                Statuses = Status,
                FromDate = FromDate,
                ToDate = ToDate
            };

            var csvBytes = await _orderService.ExportSellerReportToCsvAsync(Store.Id, query);

            if (csvBytes.Length == 0)
            {
                TempData["Warning"] = "No orders match your current filter criteria. Please adjust your filters and try again.";
                return RedirectToPage();
            }

            var fileName = $"revenue-report-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.csv";
            return File(csvBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report for seller {SellerId}", sellerId);
            TempData["Error"] = "An error occurred while exporting your report.";
            return RedirectToPage();
        }
    }

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

        return "?" + string.Join("&", queryParams);
    }

    /// <summary>
    /// Gets the query string for export link that preserves filter state.
    /// </summary>
    /// <returns>The query string for export.</returns>
    public string GetExportQueryString()
    {
        var queryParams = new List<string> { "handler=Export" };

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

        return "?" + string.Join("&", queryParams);
    }

    /// <summary>
    /// Gets the CSS class for a seller sub-order status badge.
    /// </summary>
    /// <param name="status">The seller sub-order status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(SellerSubOrderStatus status) => status switch
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
    public static string GetStatusDisplayText(SellerSubOrderStatus status) => status switch
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

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
