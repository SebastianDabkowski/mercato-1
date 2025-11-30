using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Reports;

/// <summary>
/// Page model for the admin order and revenue report page with filtering and CSV export.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IOrderRevenueReportService _reportService;
    private readonly ILogger<IndexModel> _logger;
    private const int DefaultPageSize = 20;
    
    /// <summary>
    /// Maximum number of rows that can be exported to CSV.
    /// This should match the service's MaxExportRows constant.
    /// </summary>
    private const int MaxExportPageSize = 10000;

    /// <summary>
    /// Number of characters to display for truncated order IDs.
    /// </summary>
    public const int OrderIdDisplayLength = 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="reportService">The order revenue report service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IOrderRevenueReportService reportService,
        ILogger<IndexModel> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of report rows for the current page.
    /// </summary>
    public IReadOnlyList<OrderRevenueReportRow> Rows { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total number of rows matching the filter.
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
    /// Gets the total order value across all matching rows.
    /// </summary>
    public decimal TotalOrderValue { get; private set; }

    /// <summary>
    /// Gets the total commission across all matching rows.
    /// </summary>
    public decimal TotalCommission { get; private set; }

    /// <summary>
    /// Gets the total payout amount across all matching rows.
    /// </summary>
    public decimal TotalPayoutAmount { get; private set; }

    /// <summary>
    /// Gets the list of sellers for the dropdown filter.
    /// </summary>
    public IReadOnlyList<(Guid SellerId, string SellerName)> Sellers { get; private set; } = [];

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
    /// Gets or sets the selected seller ID for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the selected order statuses for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public List<OrderStatus> OrderStatuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the selected payment statuses for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public List<PaymentStatus> PaymentStatuses { get; set; } = [];

    /// <summary>
    /// Gets all available order statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<OrderStatus> AllOrderStatuses => Enum.GetValues<OrderStatus>();

    /// <summary>
    /// Gets all available payment statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<PaymentStatus> AllPaymentStatuses => Enum.GetValues<PaymentStatus>();

    /// <summary>
    /// Handles GET requests for the admin reports page with filtering.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        // Load sellers for dropdown
        Sellers = await _reportService.GetDistinctSellersAsync();

        var query = new OrderRevenueReportFilterQuery
        {
            FromDate = FromDate,
            ToDate = ToDate,
            SellerId = SellerId,
            OrderStatuses = OrderStatuses,
            PaymentStatuses = PaymentStatuses,
            Page = page,
            PageSize = DefaultPageSize
        };

        var result = await _reportService.GetReportAsync(query);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Rows = result.Rows;
        TotalCount = result.TotalCount;
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        PageSize = result.PageSize;
        TotalOrderValue = result.TotalOrderValue;
        TotalCommission = result.TotalCommission;
        TotalPayoutAmount = result.TotalPayoutAmount;

        return Page();
    }

    /// <summary>
    /// Handles the POST request for CSV export.
    /// </summary>
    /// <returns>The file result with the CSV content.</returns>
    public async Task<IActionResult> OnPostExportAsync()
    {
        var query = new OrderRevenueReportFilterQuery
        {
            FromDate = FromDate,
            ToDate = ToDate,
            SellerId = SellerId,
            OrderStatuses = OrderStatuses,
            PaymentStatuses = PaymentStatuses,
            Page = 1,
            PageSize = MaxExportPageSize
        };

        var result = await _reportService.ExportToCsvAsync(query);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            // Reload the page with data
            await OnGetAsync(1);
            return Page();
        }

        return File(result.CsvContent, "text/csv", result.FileName);
    }

    /// <summary>
    /// Gets the CSS class for an order status badge.
    /// </summary>
    /// <param name="status">The order status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetOrderStatusBadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.New => "bg-warning text-dark",
        OrderStatus.Paid => "bg-info",
        OrderStatus.Preparing => "bg-primary",
        OrderStatus.Shipped => "bg-secondary",
        OrderStatus.Delivered => "bg-success",
        OrderStatus.Cancelled => "bg-danger",
        OrderStatus.Refunded => "bg-dark",
        OrderStatus.Failed => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the CSS class for a payment status badge.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetPaymentStatusBadgeClass(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "bg-warning text-dark",
        PaymentStatus.Processing => "bg-info",
        PaymentStatus.Paid => "bg-success",
        PaymentStatus.Failed => "bg-danger",
        PaymentStatus.Cancelled => "bg-dark",
        PaymentStatus.Refunded => "bg-secondary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for an order status.
    /// </summary>
    /// <param name="status">The order status.</param>
    /// <returns>The display text.</returns>
    public static string GetOrderStatusDisplayText(OrderStatus status) => status switch
    {
        OrderStatus.New => "New",
        OrderStatus.Paid => "Paid",
        OrderStatus.Preparing => "Preparing",
        OrderStatus.Shipped => "Shipped",
        OrderStatus.Delivered => "Delivered",
        OrderStatus.Cancelled => "Cancelled",
        OrderStatus.Refunded => "Refunded",
        OrderStatus.Failed => "Failed",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the display text for a payment status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <returns>The display text.</returns>
    public static string GetPaymentStatusDisplayText(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Pending",
        PaymentStatus.Processing => "Processing",
        PaymentStatus.Paid => "Paid",
        PaymentStatus.Failed => "Failed",
        PaymentStatus.Cancelled => "Cancelled",
        PaymentStatus.Refunded => "Refunded",
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

        if (FromDate.HasValue)
        {
            queryParams.Add($"FromDate={FromDate.Value:yyyy-MM-dd}");
        }

        if (ToDate.HasValue)
        {
            queryParams.Add($"ToDate={ToDate.Value:yyyy-MM-dd}");
        }

        if (SellerId.HasValue)
        {
            queryParams.Add($"SellerId={SellerId.Value}");
        }

        foreach (var status in OrderStatuses)
        {
            queryParams.Add($"OrderStatuses={status}");
        }

        foreach (var status in PaymentStatuses)
        {
            queryParams.Add($"PaymentStatuses={status}");
        }

        return "?" + string.Join("&", queryParams);
    }
}
