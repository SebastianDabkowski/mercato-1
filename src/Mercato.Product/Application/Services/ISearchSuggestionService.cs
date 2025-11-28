using Mercato.Product.Application.Queries;

namespace Mercato.Product.Application.Services;

/// <summary>
/// Service interface for search suggestion operations.
/// </summary>
public interface ISearchSuggestionService
{
    /// <summary>
    /// Gets search suggestions for the given query.
    /// </summary>
    /// <param name="query">The search suggestion query parameters.</param>
    /// <returns>A result containing product and category suggestions.</returns>
    Task<SearchSuggestionResult> GetSuggestionsAsync(SearchSuggestionQuery query);
}
