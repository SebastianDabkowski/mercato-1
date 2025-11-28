namespace Mercato.Product.Application.Queries;

/// <summary>
/// Query parameters for filtering product search and category results.
/// </summary>
public class ProductFilterQuery
{
    /// <summary>
    /// Gets or sets the search query text to match against title and description.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the category name to filter by.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the minimum price filter (inclusive).
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Gets or sets the maximum price filter (inclusive).
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Gets or sets the product condition/status to filter by (e.g., "InStock", "OutOfStock").
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Gets or sets the store ID to filter by seller.
    /// </summary>
    public Guid? StoreId { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the sort option for ordering results.
    /// Default is Relevance for search, Newest for category browsing.
    /// </summary>
    public ProductSortOption SortBy { get; set; } = ProductSortOption.Relevance;

    /// <summary>
    /// Creates a copy of this filter query with modified search query.
    /// </summary>
    /// <param name="searchQuery">The new search query value.</param>
    /// <returns>A new ProductFilterQuery instance with the updated search query.</returns>
    public ProductFilterQuery WithSearchQuery(string? searchQuery)
    {
        return new ProductFilterQuery
        {
            SearchQuery = searchQuery,
            Category = Category,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            Condition = Condition,
            StoreId = StoreId,
            Page = Page,
            PageSize = PageSize,
            SortBy = SortBy
        };
    }

    /// <summary>
    /// Creates a copy of this filter query with modified category.
    /// </summary>
    /// <param name="category">The new category value.</param>
    /// <returns>A new ProductFilterQuery instance with the updated category.</returns>
    public ProductFilterQuery WithCategory(string? category)
    {
        return new ProductFilterQuery
        {
            SearchQuery = SearchQuery,
            Category = category,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            Condition = Condition,
            StoreId = StoreId,
            Page = Page,
            PageSize = PageSize,
            SortBy = SortBy
        };
    }

    /// <summary>
    /// Checks if any filter is currently applied (excluding pagination).
    /// </summary>
    /// <returns>True if any filter is applied; otherwise, false.</returns>
    public bool HasActiveFilters()
    {
        return MinPrice.HasValue ||
               MaxPrice.HasValue ||
               !string.IsNullOrWhiteSpace(Condition) ||
               StoreId.HasValue ||
               !string.IsNullOrWhiteSpace(Category);
    }

    /// <summary>
    /// Creates a new filter query with all filters cleared except search query and category.
    /// </summary>
    /// <returns>A new ProductFilterQuery with filters cleared.</returns>
    public ProductFilterQuery ClearFilters()
    {
        return new ProductFilterQuery
        {
            SearchQuery = SearchQuery,
            Category = Category,
            MinPrice = null,
            MaxPrice = null,
            Condition = null,
            StoreId = null,
            Page = 1,
            PageSize = PageSize,
            SortBy = SortBy
        };
    }
}
