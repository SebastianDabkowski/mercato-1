using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Products;

/// <summary>
/// Page model for the admin product moderation list page with search and filtering support.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IProductModerationService _productModerationService;
    private readonly ILogger<IndexModel> _logger;
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="productModerationService">The product moderation service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IProductModerationService productModerationService,
        ILogger<IndexModel> logger)
    {
        _productModerationService = productModerationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of product summaries for the current page.
    /// </summary>
    public IReadOnlyList<ProductModerationSummary> Products { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total number of products matching the filter.
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
    /// Gets the list of available categories for filtering.
    /// </summary>
    public IReadOnlyList<string> AvailableCategories { get; private set; } = [];

    /// <summary>
    /// Gets or sets the selected moderation statuses for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public List<ProductModerationStatus> ModerationStatus { get; set; } = [];

    /// <summary>
    /// Gets or sets the selected category for filtering (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the search term (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets all available moderation statuses for the filter dropdown.
    /// </summary>
    public static IEnumerable<ProductModerationStatus> AllModerationStatuses => Enum.GetValues<ProductModerationStatus>();

    /// <summary>
    /// Handles GET requests for the admin products moderation page with filtering.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        // Ensure page number is at least 1
        page = Math.Max(1, page);

        var query = new ProductModerationFilterQuery
        {
            ModerationStatuses = ModerationStatus,
            Category = Category,
            SearchTerm = SearchTerm,
            Page = page,
            PageSize = DefaultPageSize
        };

        var result = await _productModerationService.GetProductsForModerationAsync(query);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Products = result.Products;
        TotalCount = result.TotalCount;
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        PageSize = result.PageSize;
        AvailableCategories = result.AvailableCategories;

        return Page();
    }

    /// <summary>
    /// Gets the CSS class for a moderation status badge.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetModerationStatusBadgeClass(ProductModerationStatus status) => 
        ProductModerationDisplayHelpers.GetModerationStatusBadgeClass(status);

    /// <summary>
    /// Gets the display text for a moderation status.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The display text.</returns>
    public static string GetModerationStatusDisplayText(ProductModerationStatus status) => 
        ProductModerationDisplayHelpers.GetModerationStatusDisplayText(status);

    /// <summary>
    /// Gets the CSS class for a product status badge.
    /// </summary>
    /// <param name="status">The product status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetProductStatusBadgeClass(ProductStatus status) => 
        ProductModerationDisplayHelpers.GetProductStatusBadgeClass(status);

    /// <summary>
    /// Gets the display text for a product status.
    /// </summary>
    /// <param name="status">The product status.</param>
    /// <returns>The display text.</returns>
    public static string GetProductStatusDisplayText(ProductStatus status) => 
        ProductModerationDisplayHelpers.GetProductStatusDisplayText(status);

    /// <summary>
    /// Gets the query string for pagination links that preserves filter state.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <returns>The query string.</returns>
    public string GetPaginationQueryString(int page)
    {
        var queryParams = new List<string> { $"page={page}" };

        foreach (var status in ModerationStatus)
        {
            queryParams.Add($"ModerationStatus={status}");
        }

        if (!string.IsNullOrEmpty(Category))
        {
            queryParams.Add($"Category={Uri.EscapeDataString(Category)}");
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            queryParams.Add($"SearchTerm={Uri.EscapeDataString(SearchTerm)}");
        }

        return "?" + string.Join("&", queryParams);
    }
}
