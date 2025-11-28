namespace Mercato.Product.Application.Queries;

/// <summary>
/// Defines the available sorting options for product search and category results.
/// </summary>
public enum ProductSortOption
{
    /// <summary>
    /// Sort by relevance (default for search). For search queries, this prioritizes
    /// products that match the search query. For category browsing, this falls back to Newest.
    /// </summary>
    Relevance = 0,

    /// <summary>
    /// Sort by price from lowest to highest.
    /// </summary>
    PriceAsc = 1,

    /// <summary>
    /// Sort by price from highest to lowest.
    /// </summary>
    PriceDesc = 2,

    /// <summary>
    /// Sort by creation date, newest first.
    /// </summary>
    Newest = 3
}
