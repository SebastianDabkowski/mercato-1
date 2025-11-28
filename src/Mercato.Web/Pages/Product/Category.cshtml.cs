using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Product;

/// <summary>
/// Page model for browsing products by category.
/// </summary>
public class CategoryModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private const int DefaultPageSize = 12;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryModel"/> class.
    /// </summary>
    /// <param name="categoryService">The category service.</param>
    /// <param name="productService">The product service.</param>
    public CategoryModel(ICategoryService categoryService, IProductService productService)
    {
        _categoryService = categoryService;
        _productService = productService;
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

        // Load products in this category
        var (products, totalCount) = await _productService.GetProductsByCategoryAsync(Category.Name, CurrentPage, DefaultPageSize);
        Products = products;
        TotalProducts = totalCount;
        TotalPages = (int)Math.Ceiling((double)totalCount / DefaultPageSize);

        return Page();
    }
}
