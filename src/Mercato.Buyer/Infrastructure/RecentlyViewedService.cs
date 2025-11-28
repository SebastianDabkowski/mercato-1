using System.Text.Json;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Buyer.Infrastructure;

/// <summary>
/// Service implementation for recently viewed products operations.
/// </summary>
public class RecentlyViewedService : IRecentlyViewedService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<RecentlyViewedService> _logger;

    private const int DefaultMaxItems = 10;
    private const int AbsoluteMaxItems = 50;
    private static readonly string[] AllowedImagePrefixes = ["/uploads/", "/images/"];

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentlyViewedService"/> class.
    /// </summary>
    /// <param name="productRepository">The product repository.</param>
    /// <param name="logger">The logger.</param>
    public RecentlyViewedService(IProductRepository productRepository, ILogger<RecentlyViewedService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RecentlyViewedProductsResult> GetRecentlyViewedProductsAsync(IEnumerable<Guid> productIds, int maxItems = DefaultMaxItems)
    {
        var productIdList = productIds?.ToList() ?? [];

        if (productIdList.Count == 0)
        {
            return RecentlyViewedProductsResult.Empty();
        }

        // Enforce maximum items limit
        var effectiveMaxItems = Math.Min(Math.Max(1, maxItems), AbsoluteMaxItems);

        // Take only the most recent IDs up to the limit
        var idsToFetch = productIdList.Take(effectiveMaxItems).ToList();

        try
        {
            // Fetch products from the repository
            var products = await _productRepository.GetByIdsAsync(idsToFetch);

            // Filter for active, visible products only
            var activeProducts = products
                .Where(p => p.Status == ProductStatus.Active && p.ArchivedAt == null)
                .ToList();

            // Preserve the order from the original list (most recent first)
            var orderedProducts = idsToFetch
                .Select(id => activeProducts.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .Select(p => MapToDto(p!))
                .ToList();

            _logger.LogDebug(
                "Retrieved {Count} valid recently viewed products from {RequestedCount} requested IDs",
                orderedProducts.Count,
                idsToFetch.Count);

            return RecentlyViewedProductsResult.Success(orderedProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recently viewed products");
            return RecentlyViewedProductsResult.Empty();
        }
    }

    private static RecentlyViewedProductDto MapToDto(Mercato.Product.Domain.Entities.Product product)
    {
        return new RecentlyViewedProductDto
        {
            Id = product.Id,
            Title = product.Title,
            Price = product.Price,
            ImageUrl = GetFirstValidImageUrl(product.Images),
            IsInStock = product.Stock > 0
        };
    }

    private static string? GetFirstValidImageUrl(string? imagesJson)
    {
        if (string.IsNullOrEmpty(imagesJson) || imagesJson == "[]")
        {
            return null;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                MaxDepth = 2
            };
            var images = JsonSerializer.Deserialize<string[]>(imagesJson, options);

            if (images == null || images.Length == 0)
            {
                return null;
            }

            var imageUrl = images[0];
            if (IsValidImageUrl(imageUrl))
            {
                return imageUrl;
            }

            return null;
        }
        catch
        {
            return null;
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
