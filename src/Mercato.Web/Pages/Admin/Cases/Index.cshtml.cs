using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Orders.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Cases;

/// <summary>
/// Page model for the admin cases list page with search and filtering support.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IAdminCaseService _adminCaseService;
    private readonly ILogger<IndexModel> _logger;
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="adminCaseService">The admin case service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IAdminCaseService adminCaseService,
        ILogger<IndexModel> logger)
    {
        _adminCaseService = adminCaseService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of case summaries for the current page.
    /// </summary>
    public IReadOnlyList<AdminCaseSummary> Cases { get; private set; } = [];

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
    /// Gets or sets the selected case types for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public List<CaseType> Type { get; set; } = [];

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
    /// Gets or sets the search term (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets all available return statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<ReturnStatus> AllStatuses => Enum.GetValues<ReturnStatus>();

    /// <summary>
    /// Gets all available case types for the filter dropdown.
    /// </summary>
    public static IEnumerable<CaseType> AllCaseTypes => Enum.GetValues<CaseType>();

    /// <summary>
    /// Handles GET requests for the admin cases page with filtering.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        var query = new AdminCaseFilterQuery
        {
            Statuses = Status,
            CaseTypes = Type,
            FromDate = FromDate,
            ToDate = ToDate,
            SearchTerm = SearchTerm,
            Page = page,
            PageSize = DefaultPageSize
        };

        var result = await _adminCaseService.GetCasesAsync(query);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
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

    /// <summary>
    /// Gets the CSS class for a case status badge.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "bg-warning text-dark",
        ReturnStatus.UnderReview => "bg-info",
        ReturnStatus.Approved => "bg-success",
        ReturnStatus.Rejected => "bg-danger",
        ReturnStatus.Completed => "bg-secondary",
        ReturnStatus.UnderAdminReview => "bg-primary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a return status.
    /// </summary>
    /// <param name="status">The return status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "Requested",
        ReturnStatus.UnderReview => "Under Review",
        ReturnStatus.Approved => "Approved",
        ReturnStatus.Rejected => "Rejected",
        ReturnStatus.Completed => "Completed",
        ReturnStatus.UnderAdminReview => "Under Admin Review",
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
        CaseType.Return => "bg-info text-dark",
        CaseType.Complaint => "bg-warning text-dark",
        _ => "bg-secondary"
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

        foreach (var type in Type)
        {
            queryParams.Add($"Type={type}");
        }

        if (FromDate.HasValue)
        {
            queryParams.Add($"FromDate={FromDate.Value:yyyy-MM-dd}");
        }

        if (ToDate.HasValue)
        {
            queryParams.Add($"ToDate={ToDate.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            queryParams.Add($"SearchTerm={Uri.EscapeDataString(SearchTerm)}");
        }

        return "?" + string.Join("&", queryParams);
    }
}
