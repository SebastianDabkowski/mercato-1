using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Buyer;

/// <summary>
/// API page for retrieving recently viewed products.
/// </summary>
public class RecentlyViewedModel : PageModel
{
    private readonly IRecentlyViewedService _recentlyViewedService;

    /// <summary>
    /// Maximum number of product IDs that can be requested.
    /// </summary>
    public const int MaxProductIds = 50;

    /// <summary>
    /// Default maximum number of items to return.
    /// </summary>
    public const int DefaultMaxItems = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentlyViewedModel"/> class.
    /// </summary>
    /// <param name="recentlyViewedService">The recently viewed service.</param>
    public RecentlyViewedModel(IRecentlyViewedService recentlyViewedService)
    {
        _recentlyViewedService = recentlyViewedService;
    }

    /// <summary>
    /// Handles GET requests for recently viewed products.
    /// </summary>
    /// <param name="ids">Comma-separated list of product IDs in order from most recent to oldest.</param>
    /// <param name="maxItems">Maximum number of items to return.</param>
    /// <returns>JSON response with recently viewed products.</returns>
    public async Task<IActionResult> OnGetAsync(string? ids, int maxItems = DefaultMaxItems)
    {
        // Return empty result for null/empty queries
        if (string.IsNullOrWhiteSpace(ids))
        {
            return new JsonResult(new RecentlyViewedResponse
            {
                Products = []
            });
        }

        // Parse product IDs
        var productIds = ParseProductIds(ids);

        if (productIds.Count == 0)
        {
            return new JsonResult(new RecentlyViewedResponse
            {
                Products = []
            });
        }

        var result = await _recentlyViewedService.GetRecentlyViewedProductsAsync(productIds, maxItems);

        return new JsonResult(new RecentlyViewedResponse
        {
            Products = result.Products.Select(p => new RecentlyViewedProductResponse
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                IsInStock = p.IsInStock
            }).ToList()
        });
    }

    private static List<Guid> ParseProductIds(string ids)
    {
        var result = new List<Guid>();
        var parts = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts.Take(MaxProductIds))
        {
            if (Guid.TryParse(part, out var guid))
            {
                result.Add(guid);
            }
        }

        return result;
    }
}

/// <summary>
/// Response DTO for recently viewed products.
/// </summary>
public class RecentlyViewedResponse
{
    /// <summary>
    /// Gets or sets the recently viewed products.
    /// </summary>
    public IReadOnlyList<RecentlyViewedProductResponse> Products { get; set; } = [];
}

/// <summary>
/// DTO for a recently viewed product.
/// </summary>
public class RecentlyViewedProductResponse
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the product is in stock.
    /// </summary>
    public bool IsInStock { get; set; }
}
