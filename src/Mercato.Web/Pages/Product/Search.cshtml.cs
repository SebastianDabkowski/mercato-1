using Mercato.Product.Application.Queries;
using Mercato.Product.Application.Services;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Product;

/// <summary>
/// Page model for searching products by keyword with filtering support.
/// </summary>
public class SearchModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IStoreProfileService _storeProfileService;
    private const int DefaultPageSize = 12;
    private const string SearchPageBasePath = "/Product/Search";

    /// <summary>
    /// Maximum length for search queries to prevent abuse.
    /// </summary>
    public const int MaxQueryLength = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchModel"/> class.
    /// </summary>
    /// <param name="productService">The product service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="storeProfileService">The store profile service for seller filter.</param>
    public SearchModel(
        IProductService productService,
        ICategoryService categoryService,
        IStoreProfileService storeProfileService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _storeProfileService = storeProfileService;
    }

    /// <summary>
    /// Gets the search query.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets the minimum price filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Gets or sets the maximum price filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Gets or sets the condition filter (InStock, OutOfStock).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Condition { get; set; }

    /// <summary>
    /// Gets or sets the store ID filter for seller filtering.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the category filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the sort option for ordering results.
    /// Default is Relevance for search.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ProductSortOption SortBy { get; set; } = ProductSortOption.Relevance;

    /// <summary>
    /// Gets the products matching the search query.
    /// </summary>
    public IReadOnlyList<Mercato.Product.Domain.Entities.Product> Products { get; private set; } = [];

    /// <summary>
    /// Gets the root categories for navigation.
    /// </summary>
    public IReadOnlyList<Mercato.Product.Domain.Entities.Category> RootCategories { get; private set; } = [];

    /// <summary>
    /// Gets the available stores for the seller filter dropdown.
    /// </summary>
    public IReadOnlyList<StoreFilterOption> AvailableStores { get; private set; } = [];

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
    /// Gets the pagination information for UI rendering.
    /// </summary>
    public PaginationInfo Pagination { get; private set; } = PaginationInfo.Create(1, 0, 12);

    /// <summary>
    /// Gets the minimum available price for the price filter range.
    /// </summary>
    public decimal? MinAvailablePrice { get; private set; }

    /// <summary>
    /// Gets the maximum available price for the price filter range.
    /// </summary>
    public decimal? MaxAvailablePrice { get; private set; }

    /// <summary>
    /// Gets whether any filter is currently applied.
    /// </summary>
    public bool HasActiveFilters => MinPrice.HasValue || MaxPrice.HasValue || 
                                    !string.IsNullOrWhiteSpace(Condition) || 
                                    StoreId.HasValue ||
                                    !string.IsNullOrWhiteSpace(Category);

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

        // Load price range for filter UI
        var (minPrice, maxPrice) = await _productService.GetActivePriceRangeAsync();
        MinAvailablePrice = minPrice;
        MaxAvailablePrice = maxPrice;

        // Load available stores for seller filter
        await LoadAvailableStoresAsync();

        // Truncate extremely long queries to prevent abuse
        if (Query != null && Query.Length > MaxQueryLength)
        {
            Query = Query[..MaxQueryLength];
        }

        // Create filter query
        var filter = new ProductFilterQuery
        {
            SearchQuery = string.IsNullOrWhiteSpace(Query) ? null : Query,
            Category = Category,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            Condition = Condition,
            StoreId = StoreId,
            Page = CurrentPage,
            PageSize = DefaultPageSize,
            SortBy = SortBy
        };

        // Search products with filters
        var result = await _productService.SearchProductsWithFiltersAsync(filter);
        Products = result.Products;
        TotalProducts = result.TotalCount;
        TotalPages = result.TotalPages;
        Pagination = result.Pagination;

        return Page();
    }

    /// <summary>
    /// Handles POST request to clear all filters.
    /// </summary>
    /// <returns>Redirect to search page with only the query and sort preserved.</returns>
    public IActionResult OnPostClearFilters()
    {
        return RedirectToPage(new { Query, SortBy });
    }

    /// <summary>
    /// Generates a return URL that includes the current search parameters for navigation back to this page.
    /// </summary>
    /// <returns>The return URL with all current filter and pagination parameters.</returns>
    public string GetReturnUrl()
    {
        var queryParams = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(Query))
        {
            queryParams.Add($"query={Uri.EscapeDataString(Query)}");
        }
        if (!string.IsNullOrWhiteSpace(Category))
        {
            queryParams.Add($"category={Uri.EscapeDataString(Category)}");
        }
        if (MinPrice.HasValue)
        {
            queryParams.Add($"minPrice={MinPrice.Value}");
        }
        if (MaxPrice.HasValue)
        {
            queryParams.Add($"maxPrice={MaxPrice.Value}");
        }
        if (!string.IsNullOrWhiteSpace(Condition))
        {
            queryParams.Add($"condition={Uri.EscapeDataString(Condition)}");
        }
        if (StoreId.HasValue)
        {
            queryParams.Add($"storeId={StoreId.Value}");
        }
        if (SortBy != ProductSortOption.Relevance)
        {
            queryParams.Add($"sortBy={SortBy}");
        }
        if (CurrentPage > 1)
        {
            queryParams.Add($"page={CurrentPage}");
        }

        var returnUrl = SearchPageBasePath;
        if (queryParams.Count > 0)
        {
            returnUrl += "?" + string.Join("&", queryParams);
        }

        return returnUrl;
    }

    /// <summary>
    /// Loads the available stores for the seller filter dropdown.
    /// </summary>
    private async Task LoadAvailableStoresAsync()
    {
        var storeIds = await _productService.GetActiveProductStoreIdsAsync();
        if (storeIds.Count == 0)
        {
            AvailableStores = [];
            return;
        }

        var stores = await _storeProfileService.GetStoresByIdsAsync(storeIds);
        AvailableStores = stores
            .Select(s => new StoreFilterOption { Id = s.Id, Name = s.Name })
            .OrderBy(s => s.Name)
            .ToList();
    }
}
