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
/// Page model for creating a new product in the seller's catalog.
/// </summary>
[Authorize(Roles = "Seller")]
public class CreateModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        ILogger<CreateModel> logger)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    [BindProperty]
    [StringLength(2000, ErrorMessage = "Description must be at most 2000 characters.")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the available stock quantity.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Stock is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Category is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 100 characters.")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product weight in kilograms.
    /// </summary>
    [BindProperty]
    [Range(0, 1000, ErrorMessage = "Weight must be between 0 and 1000 kg.")]
    public decimal? Weight { get; set; }

    /// <summary>
    /// Gets or sets the product length in centimeters.
    /// </summary>
    [BindProperty]
    [Range(0, 500, ErrorMessage = "Length must be between 0 and 500 cm.")]
    public decimal? Length { get; set; }

    /// <summary>
    /// Gets or sets the product width in centimeters.
    /// </summary>
    [BindProperty]
    [Range(0, 500, ErrorMessage = "Width must be between 0 and 500 cm.")]
    public decimal? Width { get; set; }

    /// <summary>
    /// Gets or sets the product height in centimeters.
    /// </summary>
    [BindProperty]
    [Range(0, 500, ErrorMessage = "Height must be between 0 and 500 cm.")]
    public decimal? Height { get; set; }

    /// <summary>
    /// Gets or sets the available shipping methods for this product.
    /// </summary>
    [BindProperty]
    [StringLength(500, ErrorMessage = "Shipping methods must be at most 500 characters.")]
    public string? ShippingMethods { get; set; }

    /// <summary>
    /// Gets or sets the product images as a JSON array of image URLs.
    /// </summary>
    [BindProperty]
    [StringLength(4000, ErrorMessage = "Images must be at most 4000 characters.")]
    public string? Images { get; set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets whether the seller has a store configured.
    /// </summary>
    public bool HasStore { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            return RedirectToPage("/Seller/Login");
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            HasStore = store != null;

            if (!HasStore)
            {
                ErrorMessage = "You must configure your store profile before adding products.";
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading store for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your store information. Please try again.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            return RedirectToPage("/Seller/Login");
        }

        if (!ModelState.IsValid)
        {
            HasStore = true;
            return Page();
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                HasStore = false;
                ErrorMessage = "You must configure your store profile before adding products.";
                return Page();
            }

            HasStore = true;

            var command = new CreateProductCommand
            {
                StoreId = store.Id,
                Title = Title,
                Description = Description,
                Price = Price,
                Stock = Stock,
                Category = Category,
                Weight = Weight,
                Length = Length,
                Width = Width,
                Height = Height,
                ShippingMethods = ShippingMethods,
                Images = Images
            };

            var result = await _productService.CreateProductAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Product {ProductId} created for store {StoreId}", result.ProductId, store.Id);
                return RedirectToPage("Index", new { success = true });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product for seller {SellerId}", sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while creating the product. Please try again.");
            HasStore = true;
            return Page();
        }
    }
}
