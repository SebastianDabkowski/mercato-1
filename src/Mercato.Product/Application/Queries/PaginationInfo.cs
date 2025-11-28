namespace Mercato.Product.Application.Queries;

/// <summary>
/// Provides pagination information and computes visible page numbers for pagination UI.
/// </summary>
public class PaginationInfo
{
    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Gets the total count of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets whether there are multiple pages of results.
    /// </summary>
    public bool HasMultiplePages => TotalPages > 1;

    /// <summary>
    /// Gets whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Gets whether there is a next page available.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Gets whether this is the last page of results.
    /// </summary>
    public bool IsLastPage => CurrentPage >= TotalPages;

    /// <summary>
    /// Gets the previous page number (or 1 if on first page).
    /// </summary>
    public int PreviousPage => CurrentPage > 1 ? CurrentPage - 1 : 1;

    /// <summary>
    /// Gets the next page number (or last page if on last page).
    /// </summary>
    public int NextPage => CurrentPage < TotalPages ? CurrentPage + 1 : TotalPages;

    /// <summary>
    /// Gets the 1-based index of the first item on the current page.
    /// </summary>
    public int FirstItemIndex => TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;

    /// <summary>
    /// Gets the 1-based index of the last item on the current page.
    /// </summary>
    public int LastItemIndex => TotalCount == 0 ? 0 : Math.Min(CurrentPage * PageSize, TotalCount);

    /// <summary>
    /// Maximum number of page numbers to display in the pagination window.
    /// </summary>
    private const int MaxVisiblePages = 7;

    /// <summary>
    /// Gets the visible page numbers for the pagination UI.
    /// For small page counts, returns all pages. For large page counts,
    /// returns a windowed set with the first page, last page, current page
    /// with neighbors, and null values representing ellipses.
    /// </summary>
    /// <returns>A list of page numbers (or null for ellipsis indicators).</returns>
    public IReadOnlyList<int?> GetVisiblePageNumbers()
    {
        if (TotalPages <= MaxVisiblePages)
        {
            // Show all pages when total is small
            return Enumerable.Range(1, TotalPages).Select(p => (int?)p).ToList();
        }

        var pages = new List<int?>();

        // Always show first page
        pages.Add(1);

        // Calculate window around current page
        int windowStart = Math.Max(2, CurrentPage - 1);
        int windowEnd = Math.Min(TotalPages - 1, CurrentPage + 1);

        // Adjust window to ensure we have enough visible pages
        if (windowStart > 2)
        {
            pages.Add(null); // Ellipsis after first page
        }

        // Add pages in the window
        for (int i = windowStart; i <= windowEnd; i++)
        {
            pages.Add(i);
        }

        // Add ellipsis before last page if needed
        if (windowEnd < TotalPages - 1)
        {
            pages.Add(null); // Ellipsis before last page
        }

        // Always show last page
        pages.Add(TotalPages);

        return pages;
    }

    /// <summary>
    /// Creates a new PaginationInfo instance.
    /// </summary>
    /// <param name="currentPage">The current page number (1-based).</param>
    /// <param name="totalCount">The total count of items.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A new PaginationInfo instance.</returns>
    public static PaginationInfo Create(int currentPage, int totalCount, int pageSize)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        return new PaginationInfo
        {
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize
        };
    }
}
