using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Orders.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Reviews;

/// <summary>
/// Page model for the admin reviews list page with search and filtering support.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IReviewModerationService _reviewModerationService;
    private readonly ILogger<IndexModel> _logger;
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="reviewModerationService">The review moderation service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IReviewModerationService reviewModerationService,
        ILogger<IndexModel> logger)
    {
        _reviewModerationService = reviewModerationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of review summaries for the current page.
    /// </summary>
    public IReadOnlyList<AdminReviewSummary> Reviews { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total number of reviews matching the filter.
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
    public List<ReviewStatus> Status { get; set; } = [];

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
    /// Gets all available review statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<ReviewStatus> AllStatuses => Enum.GetValues<ReviewStatus>();

    /// <summary>
    /// Handles GET requests for the admin reviews page with filtering.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        var query = new AdminReviewFilterQuery
        {
            Statuses = Status,
            FromDate = FromDate,
            ToDate = ToDate,
            SearchTerm = SearchTerm,
            Page = page,
            PageSize = DefaultPageSize
        };

        var result = await _reviewModerationService.GetReviewsAsync(query);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Reviews = result.Reviews;
        TotalCount = result.TotalCount;
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        PageSize = result.PageSize;

        return Page();
    }

    /// <summary>
    /// Gets the CSS class for a review status badge.
    /// </summary>
    /// <param name="status">The review status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetStatusBadgeClass(ReviewStatus status) => status switch
    {
        ReviewStatus.Pending => "bg-warning text-dark",
        ReviewStatus.Published => "bg-success",
        ReviewStatus.Hidden => "bg-secondary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a review status.
    /// </summary>
    /// <param name="status">The review status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(ReviewStatus status) => status switch
    {
        ReviewStatus.Pending => "Pending",
        ReviewStatus.Published => "Published",
        ReviewStatus.Hidden => "Hidden",
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

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            queryParams.Add($"SearchTerm={Uri.EscapeDataString(SearchTerm)}");
        }

        return "?" + string.Join("&", queryParams);
    }

    /// <summary>
    /// Gets a star rating display as HTML.
    /// </summary>
    /// <param name="rating">The rating from 1 to 5.</param>
    /// <returns>The star display string.</returns>
    public static string GetStarRating(int rating)
    {
        var filled = new string('★', rating);
        var empty = new string('☆', 5 - rating);
        return filled + empty;
    }
}
