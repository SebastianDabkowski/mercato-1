using System.Security.Claims;
using System.Text.Json;
using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Services;
using Mercato.Orders.Application.Commands;
using Mercato.Orders.Application.Services;
using Mercato.Orders.Domain.Entities;
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
    private readonly IProductReviewService _productReviewService;
    private readonly ILogger<DetailsModel> _logger;

    private const string PlaceholderImage = "/images/placeholder.png";
    private const string GuestCartCookieName = "GuestCartId";
    private const int DefaultReviewsPageSize = 10;
    private static readonly string[] AllowedImagePrefixes = ["/uploads/", "/images/"];

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="productService">The product service.</param>
    /// <param name="categoryService">The category service.</param>
    /// <param name="storeProfileService">The store profile service.</param>
    /// <param name="cartService">The cart service.</param>
    /// <param name="productReviewService">The product review service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(
        IProductService productService,
        ICategoryService categoryService,
        IStoreProfileService storeProfileService,
        ICartService cartService,
        IProductReviewService productReviewService,
        ILogger<DetailsModel> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _storeProfileService = storeProfileService;
        _cartService = cartService;
        _productReviewService = productReviewService;
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
    /// Gets the paginated reviews result for the product.
    /// </summary>
    public GetProductReviewsPagedResult? ReviewsResult { get; private set; }

    /// <summary>
    /// Gets or sets the referrer URL for "Back to results" navigation.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the reviews page number (1-based).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int ReviewsPage { get; set; } = 1;

    /// <summary>
    /// Gets or sets the reviews sort option.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReviewsSortBy { get; set; }

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

        // Load product reviews with pagination and sorting
        await LoadProductReviewsAsync(id);

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
        if (quantity <= 0)
        {
            TempData["CartError"] = "Quantity must be at least 1.";
            return RedirectToPage(new { id });
        }

        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        AddToCartResult result;

        if (!string.IsNullOrEmpty(buyerId))
        {
            // Authenticated user - add to user cart
            result = await _cartService.AddToCartAsync(new AddToCartCommand
            {
                BuyerId = buyerId,
                ProductId = productId,
                Quantity = quantity
            });
        }
        else
        {
            // Guest user - add to guest cart
            var guestCartId = GetOrCreateGuestCartId();
            result = await _cartService.AddToGuestCartAsync(new AddToCartCommand
            {
                GuestCartId = guestCartId,
                ProductId = productId,
                Quantity = quantity
            });
        }

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

    /// <summary>
    /// Pre-computed star rating strings for performance.
    /// </summary>
    private static readonly string[] StarRatings =
    [
        "☆☆☆☆☆", // 0 stars
        "★☆☆☆☆", // 1 star
        "★★☆☆☆", // 2 stars
        "★★★☆☆", // 3 stars
        "★★★★☆", // 4 stars
        "★★★★★"  // 5 stars
    ];

    /// <summary>
    /// Generates a star rating display string.
    /// </summary>
    /// <param name="rating">The rating value (1-5).</param>
    /// <returns>A string of filled and empty star icons.</returns>
    public static string GetStarRating(int rating)
    {
        var clampedRating = Math.Clamp(rating, 0, 5);
        return StarRatings[clampedRating];
    }

    private async Task LoadProductReviewsAsync(Guid productId)
    {
        // Parse the sort option
        var sortBy = ParseSortOption(ReviewsSortBy);

        // Ensure valid page number
        var page = Math.Max(1, ReviewsPage);

        var query = new GetProductReviewsQuery
        {
            ProductId = productId,
            Page = page,
            PageSize = DefaultReviewsPageSize,
            SortBy = sortBy
        };

        ReviewsResult = await _productReviewService.GetReviewsByProductIdPagedAsync(query);
    }

    private static ReviewSortOption ParseSortOption(string? sortBy)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "highest" => ReviewSortOption.HighestRating,
            "lowest" => ReviewSortOption.LowestRating,
            _ => ReviewSortOption.Newest
        };
    }

    private string GetOrCreateGuestCartId()
    {
        var guestCartId = Request.Cookies[GuestCartCookieName];
        if (string.IsNullOrEmpty(guestCartId))
        {
            guestCartId = Guid.NewGuid().ToString();
            Response.Cookies.Append(GuestCartCookieName, guestCartId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }
        return guestCartId;
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
