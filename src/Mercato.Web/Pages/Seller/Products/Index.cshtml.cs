using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Products;

/// <summary>
/// Page model for listing products in the seller's catalog.
/// </summary>
[Authorize(Roles = "Seller")]
public class IndexModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        ILogger<IndexModel> logger)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of products for the seller's store.
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
    /// Gets whether a product was successfully created.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Success { get; set; }

    /// <summary>
    /// Gets whether the seller has a store configured.
    /// </summary>
    public bool HasStore { get; private set; }

    public async Task OnGetAsync()
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

            Products = await _productService.GetProductsByStoreIdAsync(store.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products for seller {SellerId}", sellerId);
            HasError = true;
            ErrorMessage = "An error occurred while loading your products. Please try again.";
        }
    }

    /// <summary>
    /// Gets the CSS class for a product status badge.
    /// </summary>
    /// <param name="status">The product status.</param>
    /// <returns>The CSS class for the badge.</returns>
    public static string GetStatusBadgeClass(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.Draft => "bg-secondary",
            ProductStatus.Active => "bg-success",
            ProductStatus.Inactive => "bg-warning",
            ProductStatus.OutOfStock => "bg-danger",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Gets the display name for a product status.
    /// </summary>
    /// <param name="status">The product status.</param>
    /// <returns>The display name for the status.</returns>
    public static string GetStatusDisplayName(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.Draft => "Draft",
            ProductStatus.Active => "Active",
            ProductStatus.Inactive => "Inactive",
            ProductStatus.OutOfStock => "Out of Stock",
            _ => "Unknown"
        };
    }
}
