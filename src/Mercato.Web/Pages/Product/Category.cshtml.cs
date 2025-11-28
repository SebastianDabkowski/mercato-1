using Mercato.Product.Application.Queries;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Product;

/// <summary>
/// Page model for browsing products by category with filtering support.
/// </summary>
public class CategoryModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private const int DefaultPageSize = 12;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryModel"/> class.
    /// </summary>
    /// <param name="categoryService">The category service.</param>
    /// <param name="productService">The product service.</param>
    /// <param name="storeProfileService">The store profile service for seller filter.</param>
    public CategoryModel(
        ICategoryService categoryService,
        IProductService productService,
        IStoreProfileService storeProfileService)
    {
        _categoryService = categoryService;
        _productService = productService;
        _storeProfileService = storeProfileService;
    }

    /// <summary>
    /// Gets the current category.
    /// </summary>
    public Category? Category { get; private set; }

    /// <summary>
    /// Gets the parent category if this is a subcategory.
    /// </summary>
    public Category? ParentCategory { get; private set; }

    /// <summary>
    /// Gets the subcategories of the current category.
    /// </summary>
    public IReadOnlyList<Category> Subcategories { get; private set; } = [];

    /// <summary>
    /// Gets the products in the current category.
    /// </summary>
    public IReadOnlyList<Mercato.Product.Domain.Entities.Product> Products { get; private set; } = [];

    /// <summary>
    /// Gets the root categories for navigation.
    /// </summary>
    public IReadOnlyList<Category> RootCategories { get; private set; } = [];

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
                                    StoreId.HasValue;

    /// <summary>
    /// Handles GET requests for category listing page.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="page">The page number for pagination.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? id, int page = 1)
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

        // If no category specified, show all root categories
        if (!id.HasValue)
        {
            return Page();
        }

        // Load the requested category
        Category = await _categoryService.GetCategoryByIdAsync(id.Value);
        if (Category == null || !Category.IsActive)
        {
            return NotFound();
        }

        // Load parent category for breadcrumb
        if (Category.ParentId.HasValue)
        {
            ParentCategory = await _categoryService.GetCategoryByIdAsync(Category.ParentId.Value);
        }

        // Load subcategories
        Subcategories = await _categoryService.GetActiveCategoriesByParentIdAsync(Category.Id);

        // Create filter query for products in this category
        var filter = new ProductFilterQuery
        {
            SearchQuery = null,
            Category = Category.Name,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            Condition = Condition,
            StoreId = StoreId,
            Page = CurrentPage,
            PageSize = DefaultPageSize
        };

        // Load products with filters
        var result = await _productService.SearchProductsWithFiltersAsync(filter);
        Products = result.Products;
        TotalProducts = result.TotalCount;
        TotalPages = result.TotalPages;

        return Page();
    }

    /// <summary>
    /// Handles POST request to clear all filters.
    /// </summary>
    /// <param name="id">The category ID to preserve.</param>
    /// <returns>Redirect to category page with filters cleared.</returns>
    public IActionResult OnPostClearFilters(Guid? id)
    {
        return RedirectToPage(new { id });
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
