using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Photos;

/// <summary>
/// Page model for the admin photo moderation list page with filtering support.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IPhotoModerationService _photoModerationService;
    private readonly ILogger<IndexModel> _logger;
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="photoModerationService">The photo moderation service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IPhotoModerationService photoModerationService,
        ILogger<IndexModel> logger)
    {
        _photoModerationService = photoModerationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of photo summaries for the current page.
    /// </summary>
    public IReadOnlyList<PhotoModerationSummary> Photos { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total number of photos matching the filter.
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
    /// Gets or sets whether to show only flagged photos (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool FlaggedOnly { get; set; }

    /// <summary>
    /// Handles GET requests for the admin photo moderation page with filtering.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        // Ensure page number is at least 1
        page = Math.Max(1, page);

        var query = new PhotoModerationFilterQuery
        {
            FlaggedOnly = FlaggedOnly,
            Page = page,
            PageSize = DefaultPageSize
        };

        var result = await _photoModerationService.GetPhotosForModerationAsync(query);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Photos = result.Photos;
        TotalCount = result.TotalCount;
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        PageSize = result.PageSize;

        return Page();
    }

    /// <summary>
    /// Gets the CSS class for a moderation status badge.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetModerationStatusBadgeClass(PhotoModerationStatus status) =>
        PhotoModerationDisplayHelpers.GetModerationStatusBadgeClass(status);

    /// <summary>
    /// Gets the display text for a moderation status.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The display text.</returns>
    public static string GetModerationStatusDisplayText(PhotoModerationStatus status) =>
        PhotoModerationDisplayHelpers.GetModerationStatusDisplayText(status);

    /// <summary>
    /// Gets the query string for pagination links that preserves filter state.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <returns>The query string.</returns>
    public string GetPaginationQueryString(int page)
    {
        var queryParams = new List<string> { $"page={page}" };

        if (FlaggedOnly)
        {
            queryParams.Add("FlaggedOnly=true");
        }

        return "?" + string.Join("&", queryParams);
    }
}
