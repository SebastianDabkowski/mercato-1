using Mercato.Orders.Application.Queries;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Returns;

/// <summary>
/// Page model for the seller cases (returns and complaints) index page with filtering and pagination.
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
    /// Gets the list of cases for the current page.
    /// </summary>
    public IReadOnlyList<ReturnRequest> Cases { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total number of cases matching the filter.
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
    public List<ReturnStatus> Status { get; set; } = [];

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
    /// Gets all available return statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<ReturnStatus> AllStatuses => Enum.GetValues<ReturnStatus>();

    /// <summary>
    /// Handles GET requests for the seller cases index page with filtering.
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

            var query = new SellerCaseFilterQuery
            {
                StoreId = Store.Id,
                Statuses = Status,
                FromDate = FromDate,
                ToDate = ToDate,
                Page = page,
                PageSize = DefaultPageSize
            };

            var result = await _orderService.GetFilteredCasesForSellerAsync(query);

            if (!result.Succeeded)
            {
                ErrorMessage = string.Join(", ", result.Errors);
                return Page();
            }

            Cases = result.Cases;
            TotalCount = result.TotalCount;
            CurrentPage = result.Page;
            TotalPages = result.TotalPages;
            PageSize = result.PageSize;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cases for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your cases.";
            return Page();
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
    /// Gets the CSS class for a return status badge.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(ReturnStatus status) => status switch
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
    public static string GetStatusDisplayText(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "Pending Review",
        ReturnStatus.UnderReview => "Under Review",
        ReturnStatus.Approved => "Approved",
        ReturnStatus.Rejected => "Rejected",
        ReturnStatus.Completed => "Resolved",
        _ => status.ToString()
    };

    /// <summary>
    /// Gets the display text for a case type.
    /// </summary>
    /// <param name="caseType">The case type.</param>
    /// <returns>The display text.</returns>
    public static string GetCaseTypeDisplayText(CaseType caseType) => caseType switch
    {
        CaseType.Return => "Return",
        CaseType.Complaint => "Complaint",
        _ => caseType.ToString()
    };

    /// <summary>
    /// Gets the CSS class for a case type badge.
    /// </summary>
    /// <param name="caseType">The case type.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetCaseTypeBadgeClass(CaseType caseType) => caseType switch
    {
        CaseType.Return => "bg-primary",
        CaseType.Complaint => "bg-secondary",
        _ => "bg-light text-dark"
    };

    private string? GetSellerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
