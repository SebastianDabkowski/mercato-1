using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Products;

/// <summary>
/// Page model for bulk updating price and stock for multiple products.
/// </summary>
[Authorize(Roles = "Seller")]
public class BulkUpdateModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<BulkUpdateModel> _logger;

    public BulkUpdateModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        ILogger<BulkUpdateModel> logger)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of active products for selection.
    /// </summary>
    public IReadOnlyList<Mercato.Product.Domain.Entities.Product> Products { get; private set; } = [];

    /// <summary>
    /// Gets whether there was an error loading products.
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
    /// Gets or sets the selected product IDs.
    /// </summary>
    [BindProperty]
    public List<Guid> SelectedProductIds { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to update price.
    /// </summary>
    [BindProperty]
    public bool UpdatePrice { get; set; }

    /// <summary>
    /// Gets or sets the price update type.
    /// </summary>
    [BindProperty]
    public BulkPriceUpdateType PriceUpdateType { get; set; }

    /// <summary>
    /// Gets or sets the price update value.
    /// </summary>
    [BindProperty]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price value must be greater than 0.")]
    public decimal PriceValue { get; set; }

    /// <summary>
    /// Gets or sets whether to update stock.
    /// </summary>
    [BindProperty]
    public bool UpdateStock { get; set; }

    /// <summary>
    /// Gets or sets the stock update type.
    /// </summary>
    [BindProperty]
    public BulkStockUpdateType StockUpdateType { get; set; }

    /// <summary>
    /// Gets or sets the stock update value.
    /// </summary>
    [BindProperty]
    [Range(0, int.MaxValue, ErrorMessage = "Stock value must be 0 or greater.")]
    public int StockValue { get; set; }

    /// <summary>
    /// Gets the result of the last bulk update operation.
    /// </summary>
    public BulkUpdatePriceStockResult? UpdateResult { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadProductsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadProductsAsync();

        if (!HasStore || HasError)
        {
            return Page();
        }

        if (SelectedProductIds.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select at least one product to update.");
            return Page();
        }

        if (!UpdatePrice && !UpdateStock)
        {
            ModelState.AddModelError(string.Empty, "Please select at least one field to update (price or stock).");
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

            var command = new BulkUpdatePriceStockCommand
            {
                SellerId = sellerId,
                StoreId = store.Id,
                ProductIds = SelectedProductIds
            };

            if (UpdatePrice)
            {
                command.PriceUpdate = new BulkPriceUpdate
                {
                    UpdateType = PriceUpdateType,
                    Value = PriceValue
                };
            }

            if (UpdateStock)
            {
                command.StockUpdate = new BulkStockUpdate
                {
                    UpdateType = StockUpdateType,
                    Value = StockValue
                };
            }

            UpdateResult = await _productService.BulkUpdatePriceStockAsync(command);

            if (UpdateResult.Succeeded)
            {
                _logger.LogInformation(
                    "Bulk update completed for store {StoreId} by seller {SellerId}: {SuccessCount} succeeded, {FailureCount} failed",
                    store.Id,
                    sellerId,
                    UpdateResult.SuccessCount,
                    UpdateResult.FailureCount);
            }
            else if (UpdateResult.IsNotAuthorized)
            {
                return Forbid();
            }

            // Reload products to show updated values
            await LoadProductsAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk update");
            ModelState.AddModelError(string.Empty, "An error occurred while updating the products. Please try again.");
            return Page();
        }
    }

    private async Task LoadProductsAsync()
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
                ErrorMessage = "You must configure your store profile before managing products.";
                return;
            }

            Products = await _productService.GetActiveProductsByStoreIdAsync(store.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products for seller {SellerId}", sellerId);
            HasError = true;
            ErrorMessage = "An error occurred while loading your products. Please try again.";
        }
    }

    /// <summary>
    /// Gets the display name for a price update type.
    /// </summary>
    public static string GetPriceUpdateTypeDisplayName(BulkPriceUpdateType updateType)
    {
        return updateType switch
        {
            BulkPriceUpdateType.Fixed => "Set to fixed value",
            BulkPriceUpdateType.PercentageIncrease => "Increase by percentage",
            BulkPriceUpdateType.PercentageDecrease => "Decrease by percentage",
            BulkPriceUpdateType.AmountIncrease => "Increase by amount",
            BulkPriceUpdateType.AmountDecrease => "Decrease by amount",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the display name for a stock update type.
    /// </summary>
    public static string GetStockUpdateTypeDisplayName(BulkStockUpdateType updateType)
    {
        return updateType switch
        {
            BulkStockUpdateType.Fixed => "Set to fixed value",
            BulkStockUpdateType.Increase => "Increase by amount",
            BulkStockUpdateType.Decrease => "Decrease by amount",
            _ => "Unknown"
        };
    }
}
