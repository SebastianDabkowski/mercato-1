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
/// Page model for editing an existing product in the seller's catalog.
/// </summary>
[Authorize(Roles = "Seller")]
public class EditModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        ILogger<EditModel> logger)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

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
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets whether the product was found and the user has access.
    /// </summary>
    public bool HasAccess { get; private set; }

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
            if (store == null)
            {
                HasAccess = false;
                ErrorMessage = "You must configure your store profile before editing products.";
                return Page();
            }

            var product = await _productService.GetProductByIdAsync(Id);
            if (product == null)
            {
                HasAccess = false;
                ErrorMessage = "Product not found.";
                return Page();
            }

            if (product.StoreId != store.Id)
            {
                _logger.LogWarning("Seller {SellerId} attempted to edit product {ProductId} owned by another store", sellerId, Id);
                return Forbid();
            }

            if (product.Status == Mercato.Product.Domain.Entities.ProductStatus.Archived)
            {
                HasAccess = false;
                ErrorMessage = "Cannot edit an archived product.";
                return Page();
            }

            HasAccess = true;
            Title = product.Title;
            Description = product.Description;
            Price = product.Price;
            Stock = product.Stock;
            Category = product.Category;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product {ProductId} for editing", Id);
            HasAccess = false;
            ErrorMessage = "An error occurred while loading the product. Please try again.";
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
            HasAccess = true;
            return Page();
        }

        try
        {
            var store = await _storeProfileService.GetStoreBySellerIdAsync(sellerId);
            if (store == null)
            {
                HasAccess = false;
                ErrorMessage = "You must configure your store profile before editing products.";
                return Page();
            }

            var command = new UpdateProductCommand
            {
                ProductId = Id,
                SellerId = sellerId,
                StoreId = store.Id,
                Title = Title,
                Description = Description,
                Price = Price,
                Stock = Stock,
                Category = Category
            };

            var result = await _productService.UpdateProductAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Product {ProductId} updated by seller {SellerId}", Id, sellerId);
                return RedirectToPage("Index", new { updated = true });
            }

            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            HasAccess = true;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the product. Please try again.");
            HasAccess = true;
            return Page();
        }
    }
}
