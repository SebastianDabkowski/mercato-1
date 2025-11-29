using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Queries;

/// <summary>
/// Query parameters for filtering seller sub-orders.
/// </summary>
public class SellerSubOrderFilterQuery
{
    /// <summary>
    /// Gets or sets the store ID (required).
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the statuses to filter by (optional). If empty, all statuses are included.
    /// </summary>
    public IReadOnlyList<SellerSubOrderStatus> Statuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the start date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID search term for partial match (optional).
    /// </summary>
    public string? BuyerSearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 10.
    /// </summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Result of a filtered seller sub-orders query.
/// </summary>
public class GetFilteredSellerSubOrdersResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets the list of seller sub-orders for the current page.
    /// </summary>
    public IReadOnlyList<SellerSubOrder> SubOrders { get; private init; } = [];

    /// <summary>
    /// Gets the total number of sub-orders matching the filter criteria.
    /// </summary>
    public int TotalCount { get; private init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; private init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; private init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="subOrders">The sub-orders for the current page.</param>
    /// <param name="totalCount">The total number of sub-orders matching the filter.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetFilteredSellerSubOrdersResult Success(IReadOnlyList<SellerSubOrder> subOrders, int totalCount, int page, int pageSize) => new()
    {
        Succeeded = true,
        Errors = [],
        SubOrders = subOrders,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetFilteredSellerSubOrdersResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetFilteredSellerSubOrdersResult Failure(string error) => Failure([error]);
}
