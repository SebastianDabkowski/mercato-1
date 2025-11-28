namespace Mercato.Product.Application.Queries;

/// <summary>
/// Result of a filtered product search query.
/// </summary>
public class FilteredProductsResult
{
    /// <summary>
    /// Gets the list of products matching the filter criteria.
    /// </summary>
    public IReadOnlyList<Domain.Entities.Product> Products { get; init; } = [];

    /// <summary>
    /// Gets the total count of matching products (before pagination).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets the filter query that was applied to generate these results.
    /// </summary>
    public ProductFilterQuery AppliedFilter { get; init; } = new();

    /// <summary>
    /// Creates a successful result with the specified products and metadata.
    /// </summary>
    /// <param name="products">The matching products.</param>
    /// <param name="totalCount">The total count before pagination.</param>
    /// <param name="filter">The applied filter.</param>
    /// <returns>A new FilteredProductsResult instance.</returns>
    public static FilteredProductsResult Success(
        IReadOnlyList<Domain.Entities.Product> products,
        int totalCount,
        ProductFilterQuery filter)
    {
        return new FilteredProductsResult
        {
            Products = products,
            TotalCount = totalCount,
            CurrentPage = filter.Page,
            PageSize = filter.PageSize,
            AppliedFilter = filter
        };
    }

    /// <summary>
    /// Creates an empty result.
    /// </summary>
    /// <param name="filter">The applied filter.</param>
    /// <returns>A new empty FilteredProductsResult instance.</returns>
    public static FilteredProductsResult Empty(ProductFilterQuery filter)
    {
        return new FilteredProductsResult
        {
            Products = [],
            TotalCount = 0,
            CurrentPage = filter.Page,
            PageSize = filter.PageSize,
            AppliedFilter = filter
        };
    }
}

/// <summary>
/// Represents a store option for the seller filter dropdown.
/// </summary>
public class StoreFilterOption
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store name for display.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents a category option for the category filter dropdown.
/// </summary>
public class CategoryFilterOption
{
    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the category name for display.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
