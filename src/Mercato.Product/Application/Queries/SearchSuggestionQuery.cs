namespace Mercato.Product.Application.Queries;

/// <summary>
/// Query parameters for retrieving search suggestions.
/// </summary>
public class SearchSuggestionQuery
{
    /// <summary>
    /// Gets or sets the search term entered by the user.
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of product suggestions to return.
    /// </summary>
    public int MaxProductSuggestions { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of category suggestions to return.
    /// </summary>
    public int MaxCategorySuggestions { get; set; } = 3;
}
