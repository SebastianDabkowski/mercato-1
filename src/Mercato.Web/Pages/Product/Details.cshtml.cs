using System.Text.Json;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Product;

/// <summary>
/// Page model for displaying product details.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ILogger<DetailsModel> _logger;

    private const string PlaceholderImage = "/images/placeholder.png";
    private static readonly string[] AllowedImagePrefixes = ["/uploads/", "/images/"];

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="productService">The product service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(IProductService productService, ILogger<DetailsModel> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the product being viewed.
    /// </summary>
    public Mercato.Product.Domain.Entities.Product? Product { get; private set; }

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

        return Page();
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
