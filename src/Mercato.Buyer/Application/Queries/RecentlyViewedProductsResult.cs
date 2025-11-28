namespace Mercato.Buyer.Application.Queries;

/// <summary>
/// Result containing recently viewed products.
/// </summary>
public class RecentlyViewedProductsResult
{
    /// <summary>
    /// Gets or sets the list of recently viewed products.
    /// </summary>
    public IReadOnlyList<RecentlyViewedProductDto> Products { get; set; } = [];

    /// <summary>
    /// Creates a successful result with the provided products.
    /// </summary>
    /// <param name="products">The list of recently viewed products.</param>
    /// <returns>A successful result.</returns>
    public static RecentlyViewedProductsResult Success(IReadOnlyList<RecentlyViewedProductDto> products)
    {
        return new RecentlyViewedProductsResult
        {
            Products = products
        };
    }

    /// <summary>
    /// Creates an empty result when no valid products are found.
    /// </summary>
    /// <returns>An empty result.</returns>
    public static RecentlyViewedProductsResult Empty()
    {
        return new RecentlyViewedProductsResult
        {
            Products = []
        };
    }
}

/// <summary>
/// DTO representing a recently viewed product.
/// </summary>
public class RecentlyViewedProductDto
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
    /// Gets or sets the product's first image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the product is in stock.
    /// </summary>
    public bool IsInStock { get; set; }
}
