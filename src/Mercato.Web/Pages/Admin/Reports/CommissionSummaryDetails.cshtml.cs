using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Reports;

/// <summary>
/// Page model for the commission summary details (drill-down) page showing orders for a specific seller.
/// </summary>
[Authorize(Roles = "Admin")]
public class CommissionSummaryDetailsModel : PageModel
{
    private readonly ICommissionSummaryService _summaryService;
    private readonly ILogger<CommissionSummaryDetailsModel> _logger;
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Number of characters to display for truncated order IDs.
    /// </summary>
    public const int OrderIdDisplayLength = 8;

    /// <summary>
    /// Number of pages to show on each side of the current page in pagination.
    /// </summary>
    public const int PaginationWindowSize = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionSummaryDetailsModel"/> class.
    /// </summary>
    /// <param name="summaryService">The commission summary service.</param>
    /// <param name="logger">The logger.</param>
    public CommissionSummaryDetailsModel(
        ICommissionSummaryService summaryService,
        ILogger<CommissionSummaryDetailsModel> logger)
    {
        _summaryService = summaryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of order rows for the seller.
    /// </summary>
    public IReadOnlyList<OrderCommissionRow> Rows { get; private set; } = [];

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
    /// Gets the seller's unique identifier.
    /// </summary>
    public Guid SellerId { get; private set; }

    /// <summary>
    /// Gets the seller's store name.
    /// </summary>
    public string SellerName { get; private set; } = string.Empty;

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
    /// Handles GET requests for the commission summary details page.
    /// </summary>
    /// <param name="sellerId">The seller's unique identifier.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid sellerId, int page = 1)
    {
        SellerId = sellerId;
        CurrentPage = page;

        var result = await _summaryService.GetSellerOrdersAsync(
            sellerId,
            FromDate,
            ToDate,
            page,
            DefaultPageSize);

        if (result.IsNotAuthorized)
        {
            return Forbid();
        }

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Rows = result.Rows;
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
        PageSize = result.PageSize;
        SellerId = result.SellerId;
        SellerName = result.SellerName;

        return Page();
    }

    /// <summary>
    /// Gets the query string for pagination links that preserves filter state.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <returns>The query string.</returns>
    public string GetPaginationQueryString(int page)
    {
        var queryParams = new List<string>
        {
            $"sellerId={SellerId}",
            $"page={page}"
        };

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
    /// Gets the back URL to the commission summary page with preserved filters.
    /// </summary>
    /// <returns>The back URL.</returns>
    public string GetBackUrl()
    {
        var queryParams = new List<string>();

        if (FromDate.HasValue)
        {
            queryParams.Add($"FromDate={FromDate.Value:yyyy-MM-dd}");
        }

        if (ToDate.HasValue)
        {
            queryParams.Add($"ToDate={ToDate.Value:yyyy-MM-dd}");
        }

        return "./CommissionSummary" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "");
    }
}
