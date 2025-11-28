using Mercato.Product.Application.Queries;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Product.Infrastructure;

/// <summary>
/// Service implementation for search suggestion operations.
/// </summary>
public class SearchSuggestionService : ISearchSuggestionService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<SearchSuggestionService> _logger;

    /// <summary>
    /// Minimum number of characters required to trigger suggestions.
    /// </summary>
    public const int MinSearchTermLength = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSuggestionService"/> class.
    /// </summary>
    /// <param name="productRepository">The product repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="logger">The logger.</param>
    public SearchSuggestionService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ILogger<SearchSuggestionService> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SearchSuggestionResult> GetSuggestionsAsync(SearchSuggestionQuery query)
    {
        var errors = ValidateQuery(query);
        if (errors.Count > 0)
        {
            _logger.LogWarning("Invalid search suggestion query: {Errors}", string.Join(", ", errors));
            return SearchSuggestionResult.Empty();
        }

        var searchTerm = query.SearchTerm.Trim();

        // Return empty if search term is too short
        if (searchTerm.Length < MinSearchTermLength)
        {
            return SearchSuggestionResult.Empty();
        }

        try
        {
            // Fetch products and categories in parallel
            var productTask = _productRepository.SearchProductTitlesAsync(searchTerm, query.MaxProductSuggestions);
            var categoryTask = _categoryRepository.SearchCategoriesAsync(searchTerm, query.MaxCategorySuggestions);

            await Task.WhenAll(productTask, categoryTask);

            // Access results directly after WhenAll completes
            var products = productTask.Result;
            var categories = categoryTask.Result;

            var productSuggestions = products.Select(p => new ProductSuggestion
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                Category = p.Category
            }).ToList();

            var categorySuggestions = categories.Select(c => new CategorySuggestion
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            return SearchSuggestionResult.Create(productSuggestions, categorySuggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching search suggestions for term: {SearchTerm}", searchTerm);
            return SearchSuggestionResult.Empty();
        }
    }

    /// <summary>
    /// Validates the search suggestion query.
    /// </summary>
    /// <param name="query">The query to validate.</param>
    /// <returns>A list of validation errors.</returns>
    private static List<string> ValidateQuery(SearchSuggestionQuery query)
    {
        var errors = new List<string>();

        if (query.MaxProductSuggestions < 0)
        {
            errors.Add("MaxProductSuggestions must be non-negative.");
        }

        if (query.MaxCategorySuggestions < 0)
        {
            errors.Add("MaxCategorySuggestions must be non-negative.");
        }

        if (query.MaxProductSuggestions > 20)
        {
            errors.Add("MaxProductSuggestions cannot exceed 20.");
        }

        if (query.MaxCategorySuggestions > 10)
        {
            errors.Add("MaxCategorySuggestions cannot exceed 10.");
        }

        return errors;
    }
}
