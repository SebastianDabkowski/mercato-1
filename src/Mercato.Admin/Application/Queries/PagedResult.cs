namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;
}
