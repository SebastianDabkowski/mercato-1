using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Queries;

/// <summary>
/// Query for filtering and paginating admin orders.
/// </summary>
public class AdminOrderFilterQuery
{
    /// <summary>
    /// Gets or sets the list of statuses to filter by.
    /// </summary>
    public List<OrderStatus> Statuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the start date for date range filter (inclusive).
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (inclusive).
    /// </summary>
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Gets or sets the search term for order number, buyer email, or store name.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Result of getting admin orders.
/// </summary>
public class GetAdminOrdersResult
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
    /// Gets the list of orders.
    /// </summary>
    public IReadOnlyList<Order> Orders { get; private init; } = [];

    /// <summary>
    /// Gets the total count of orders matching the filter.
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
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="orders">The orders.</param>
    /// <param name="totalCount">The total count.</param>
    /// <param name="page">The current page.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetAdminOrdersResult Success(
        IReadOnlyList<Order> orders,
        int totalCount,
        int page,
        int pageSize) => new()
    {
        Succeeded = true,
        Errors = [],
        Orders = orders,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminOrdersResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetAdminOrdersResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of getting shipping status history.
/// </summary>
public class GetShippingStatusHistoryResult
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
    /// Gets the shipping status history records.
    /// </summary>
    public IReadOnlyList<ShippingStatusHistory> History { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="history">The history records.</param>
    /// <returns>A successful result.</returns>
    public static GetShippingStatusHistoryResult Success(IReadOnlyList<ShippingStatusHistory> history) => new()
    {
        Succeeded = true,
        Errors = [],
        History = history
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetShippingStatusHistoryResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetShippingStatusHistoryResult Failure(string error) => Failure([error]);
}
