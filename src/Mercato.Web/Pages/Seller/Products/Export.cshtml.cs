using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Products;

/// <summary>
/// Page model for exporting products to CSV/XLS files.
/// </summary>
[Authorize(Roles = "Seller")]
public class ExportModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<ExportModel> _logger;

    public ExportModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        ILogger<ExportModel> logger)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets whether there was an error loading the page.
    /// </summary>
    public bool HasError { get; private set; }

    /// <summary>
    /// Gets the error message if an error occurred.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets whether the seller has a store configured.
    /// </summary>
    public bool HasStore { get; private set; }

    /// <summary>
    /// Gets the list of categories for filtering.
    /// </summary>
    public IReadOnlyList<string> Categories { get; private set; } = [];

    /// <summary>
    /// Gets or sets the export format (CSV or Excel).
    /// </summary>
    [BindProperty]
    public ExportFormat ExportFormat { get; set; } = ExportFormat.Csv;

    /// <summary>
    /// Gets or sets whether to apply filters to the export.
    /// </summary>
    [BindProperty]
    public bool ApplyFilters { get; set; }

    /// <summary>
    /// Gets or sets the search query filter.
    /// </summary>
    [BindProperty]
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the category filter.
    /// </summary>
    [BindProperty]
    public string? CategoryFilter { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [BindProperty]
    public ProductStatus? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        await LoadStoreAsync();
        await LoadCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadStoreAsync();

        if (!HasStore || HasError)
        {
            return Page();
        }

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return RedirectToPage("/Seller/Login");
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                return Page();
            }

            var command = new ExportProductCatalogCommand
            {
                StoreId = store.Id,
                SellerId = sellerId,
                Format = ExportFormat,
                ApplyFilters = ApplyFilters,
                SearchQuery = SearchQuery,
                CategoryFilter = CategoryFilter,
                StatusFilter = StatusFilter
            };

            var result = await _productService.ExportProductCatalogAsync(command);

            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                await LoadCategoriesAsync();
                return Page();
            }

            _logger.LogInformation(
                "Product catalog exported by seller {SellerId}: {Count} products, format {Format}",
                sellerId, result.ExportedCount, ExportFormat);

            return File(result.FileContent!, result.ContentType!, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting product catalog");
            ModelState.AddModelError(string.Empty, "An error occurred while exporting the catalog. Please try again.");
            await LoadCategoriesAsync();
            return Page();
        }
    }

    private async Task LoadStoreAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            HasError = true;
            ErrorMessage = "Unable to identify the current user.";
            return;
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            HasStore = store != null;

            if (store == null)
            {
                ErrorMessage = "You must configure your store profile before exporting products.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading store for seller {SellerId}", sellerId);
            HasError = true;
            ErrorMessage = "An error occurred while loading your store. Please try again.";
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            return;
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                return;
            }

            var products = await _productService.GetActiveProductsByStoreIdAsync(store.Id);
            Categories = products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories for seller {SellerId}", sellerId);
        }
    }
}
