using Mercato.Product.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Product;

/// <summary>
/// Page model for searching products by keyword.
/// </summary>
public class SearchModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private const int DefaultPageSize = 12;

    /// <summary>
    /// Maximum length for search queries to prevent abuse.
    /// </summary>
    public const int MaxQueryLength = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchModel"/> class.
    /// </summary>
    /// <param name="productService">The product service.</param>
    /// <param name="categoryService">The category service.</param>
    public SearchModel(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    /// <summary>
    /// Gets the search query.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <summary>
    /// Gets the products matching the search query.
    /// </summary>
    public IReadOnlyList<Mercato.Product.Domain.Entities.Product> Products { get; private set; } = [];

    /// <summary>
    /// Gets the root categories for navigation.
    /// </summary>
    public IReadOnlyList<Mercato.Product.Domain.Entities.Category> RootCategories { get; private set; } = [];

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int CurrentPage { get; private set; } = 1;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// Gets the total count of products.
    /// </summary>
    public int TotalProducts { get; private set; }

    /// <summary>
    /// Handles GET requests for search results page.
    /// </summary>
    /// <param name="page">The page number for pagination.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }
        CurrentPage = page;

        // Load root categories for navigation sidebar
        RootCategories = await _categoryService.GetActiveCategoriesByParentIdAsync(null);

        // If no query provided or empty, return empty results
        if (string.IsNullOrWhiteSpace(Query))
        {
            return Page();
        }

        // Truncate extremely long queries to prevent abuse
        if (Query.Length > MaxQueryLength)
        {
            Query = Query[..MaxQueryLength];
        }

        // Search products
        var (products, totalCount) = await _productService.SearchProductsAsync(Query, CurrentPage, DefaultPageSize);
        Products = products;
        TotalProducts = totalCount;
        TotalPages = (int)Math.Ceiling((double)totalCount / DefaultPageSize);

        return Page();
    }
}
