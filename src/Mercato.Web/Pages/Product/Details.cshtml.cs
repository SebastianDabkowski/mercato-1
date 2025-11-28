using System.Security.Claims;
using System.Text.Json;
using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Services;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Seller.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Product;

/// <summary>
/// Page model for displaying product details.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ICartService _cartService;
    private readonly ILogger<DetailsModel> _logger;

    private const string PlaceholderImage = "/images/placeholder.png";
    private static readonly string[] AllowedImagePrefixes = ["/uploads/", "/images/"];

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="productService">The product service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="cartService">The cart service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IProductService productService,
        ICategoryService categoryService,
        IStoreProfileService storeProfileService,
        ICartService cartService,
        ILogger<DetailsModel> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _storeProfileService = storeProfileService;
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the product being viewed.
    /// </summary>
    public Mercato.Product.Domain.Entities.Product? Product { get; private set; }

    /// <summary>
    /// Gets the category for the product.
    /// </summary>
    public Category? ProductCategory { get; private set; }

    /// <summary>
    /// Gets the store that sells the product.
    /// </summary>
    public Mercato.Seller.Domain.Entities.Store? Store { get; private set; }

    /// <summary>
    /// Gets or sets the referrer URL for "Back to results" navigation.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Handles GET requests for the product details page.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Product = await _productService.GetProductByIdAsync(id);

        // Only show active, non-archived products
        if (Product == null || Product.Status != ProductStatus.Active || Product.ArchivedAt != null)
        {
            _logger.LogInformation("Product {ProductId} not found or not available", id);
            Product = null;
            return Page();
        }

        // Load category if the product has a category assigned
        if (!string.IsNullOrEmpty(Product.Category))
        {
            ProductCategory = await _categoryService.GetCategoryByNameAsync(Product.Category);
        }

        // Load store information
        Store = await _storeProfileService.GetStoreByIdAsync(Product.StoreId);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to add a product to the cart.
    /// </summary>
    /// <param name="id">The product ID from the route.</param>
    /// <param name="productId">The product ID from the form.</param>
    /// <param name="quantity">The quantity to add.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAddToCartAsync(Guid id, Guid productId, int quantity)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(buyerId))
        {
            return RedirectToPage("/Account/Login", new { ReturnUrl = $"/Product/Details/{id}" });
        }

        if (quantity <= 0)
        {
            TempData["CartError"] = "Quantity must be at least 1.";
            return RedirectToPage(new { id });
        }

        var result = await _cartService.AddToCartAsync(new AddToCartCommand
        {
            BuyerId = buyerId,
            ProductId = productId,
            Quantity = quantity
        });

        if (result.Succeeded)
        {
            if (result.ItemAlreadyExists)
            {
                TempData["CartSuccess"] = "Product quantity updated in your cart.";
            }
            else
            {
                TempData["CartSuccess"] = "Product added to your cart.";
            }
        }
        else
        {
            TempData["CartError"] = string.Join(", ", result.Errors);
        }

        return RedirectToPage(new { id });
    }

    /// <summary>
    /// Gets the first valid image URL for the product.
    /// </summary>
    /// <returns>The first valid image URL or a placeholder.</returns>
    public string GetFirstImageUrl()
    {
        if (Product == null || string.IsNullOrEmpty(Product.Images) || Product.Images == "[]")
        {
            return PlaceholderImage;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                MaxDepth = 2
            };
            var images = JsonSerializer.Deserialize<string[]>(Product.Images, options);

            if (images == null || images.Length == 0)
            {
                return PlaceholderImage;
            }

            var imageUrl = images[0];
            if (IsValidImageUrl(imageUrl))
            {
                return imageUrl;
            }

            return PlaceholderImage;
        }
        catch
        {
            return PlaceholderImage;
        }
    }

    private static bool IsValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        foreach (var prefix in AllowedImagePrefixes)
        {
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
