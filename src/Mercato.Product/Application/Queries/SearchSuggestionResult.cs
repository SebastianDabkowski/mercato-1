namespace Mercato.Product.Application.Queries;

/// <summary>
/// Result containing search suggestions for products and categories.
/// </summary>
public class SearchSuggestionResult
{
    /// <summary>
    /// Gets the list of product title suggestions.
    /// </summary>
    public IReadOnlyList<ProductSuggestion> Products { get; init; } = [];

    /// <summary>
    /// Gets the list of category suggestions.
    /// </summary>
    public IReadOnlyList<CategorySuggestion> Categories { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether any suggestions are available.
    /// </summary>
    public bool HasSuggestions => Products.Count > 0 || Categories.Count > 0;

    /// <summary>
    /// Creates an empty result with no suggestions.
    /// </summary>
    /// <returns>An empty SearchSuggestionResult.</returns>
    public static SearchSuggestionResult Empty() => new();

    /// <summary>
    /// Creates a result with the specified suggestions.
    /// </summary>
    /// <param name="products">The product suggestions.</param>
    /// <param name="categories">The category suggestions.</param>
    /// <returns>A SearchSuggestionResult with the specified suggestions.</returns>
    public static SearchSuggestionResult Create(
        IReadOnlyList<ProductSuggestion> products,
        IReadOnlyList<CategorySuggestion> categories)
    {
        return new SearchSuggestionResult
        {
            Products = products,
            Categories = categories
        };
    }
}

/// <summary>
/// Represents a product suggestion for search autocomplete.
/// </summary>
public class ProductSuggestion
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
    /// Gets or sets the product price for display.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Represents a category suggestion for search autocomplete.
/// </summary>
public class CategorySuggestion
{
    /// <summary>
    /// Gets or sets the category ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
