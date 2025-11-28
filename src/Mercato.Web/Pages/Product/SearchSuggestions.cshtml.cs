using Mercato.Product.Application.Queries;
using Mercato.Product.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Product;

/// <summary>
/// API page for retrieving search suggestions.
/// </summary>
public class SearchSuggestionsModel : PageModel
{
    private readonly ISearchSuggestionService _searchSuggestionService;

    /// <summary>
    /// Maximum length for search term to prevent abuse.
    /// </summary>
    public const int MaxSearchTermLength = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSuggestionsModel"/> class.
    /// </summary>
    /// <param name="searchSuggestionService">The search suggestion service.</param>
    public SearchSuggestionsModel(ISearchSuggestionService searchSuggestionService)
    {
        _searchSuggestionService = searchSuggestionService;
    }

    /// <summary>
    /// Handles GET requests for search suggestions.
    /// </summary>
    /// <param name="q">The search query term.</param>
    /// <returns>JSON response with product and category suggestions.</returns>
    public async Task<IActionResult> OnGetAsync(string? q)
    {
        // Return empty result for null/empty queries
        if (string.IsNullOrWhiteSpace(q))
        {
            return new JsonResult(new SearchSuggestionResponse
            {
                Products = [],
                Categories = []
            });
        }

        // Truncate long queries to prevent abuse
        var searchTerm = q.Length > MaxSearchTermLength ? q[..MaxSearchTermLength] : q;

        var query = new SearchSuggestionQuery
        {
            SearchTerm = searchTerm,
            MaxProductSuggestions = 5,
            MaxCategorySuggestions = 3
        };

        var result = await _searchSuggestionService.GetSuggestionsAsync(query);

        return new JsonResult(new SearchSuggestionResponse
        {
            Products = result.Products.Select(p => new ProductSuggestionDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                Category = p.Category
            }).ToList(),
            Categories = result.Categories.Select(c => new CategorySuggestionDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList()
        });
    }
}

/// <summary>
/// Response DTO for search suggestions.
/// </summary>
public class SearchSuggestionResponse
{
    /// <summary>
    /// Gets or sets the product suggestions.
    /// </summary>
    public IReadOnlyList<ProductSuggestionDto> Products { get; set; } = [];

    /// <summary>
    /// Gets or sets the category suggestions.
    /// </summary>
    public IReadOnlyList<CategorySuggestionDto> Categories { get; set; } = [];
}

/// <summary>
/// DTO for product suggestion.
/// </summary>
public class ProductSuggestionDto
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
    /// Gets or sets the product category.
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// DTO for category suggestion.
/// </summary>
public class CategorySuggestionDto
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
