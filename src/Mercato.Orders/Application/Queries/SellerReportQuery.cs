using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Queries;

/// <summary>
/// Query parameters for filtering seller revenue reports.
/// </summary>
public class SellerReportFilterQuery
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
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 10.
    /// </summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Represents a single item in the seller revenue report.
/// </summary>
public class SellerReportItem
{
    /// <summary>
    /// Gets or sets the sub-order ID.
    /// </summary>
    public Guid SubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the sub-order number for display.
    /// </summary>
    public string SubOrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order date.
    /// </summary>
    public DateTimeOffset OrderDate { get; set; }

    /// <summary>
    /// Gets or sets the sub-order status.
    /// </summary>
    public SellerSubOrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the order value (total amount of the sub-order).
    /// </summary>
    public decimal OrderValue { get; set; }

    /// <summary>
    /// Gets or sets the commission amount charged by the platform.
    /// </summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the net amount to the seller (OrderValue - CommissionAmount).
    /// </summary>
    public decimal NetAmount { get; set; }
}

/// <summary>
/// Result of a seller revenue report query.
/// </summary>
public class GetSellerReportResult
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
    /// Gets the list of report items for the current page.
    /// </summary>
    public IReadOnlyList<SellerReportItem> Items { get; private init; } = [];

    /// <summary>
    /// Gets the total number of items matching the filter criteria.
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
    /// Gets the total order value for all items matching the filter.
    /// </summary>
    public decimal TotalOrderValue { get; private init; }

    /// <summary>
    /// Gets the total commission amount for all items matching the filter.
    /// </summary>
    public decimal TotalCommissionAmount { get; private init; }

    /// <summary>
    /// Gets the total net amount for all items matching the filter.
    /// </summary>
    public decimal TotalNetAmount { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="items">The report items for the current page.</param>
    /// <param name="totalCount">The total number of items matching the filter.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="totalOrderValue">The total order value across all filtered items.</param>
    /// <param name="totalCommissionAmount">The total commission across all filtered items.</param>
    /// <param name="totalNetAmount">The total net amount across all filtered items.</param>
    /// <returns>A successful result.</returns>
    public static GetSellerReportResult Success(
        IReadOnlyList<SellerReportItem> items,
        int totalCount,
        int page,
        int pageSize,
        decimal totalOrderValue,
        decimal totalCommissionAmount,
        decimal totalNetAmount) => new()
    {
        Succeeded = true,
        Errors = [],
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalOrderValue = totalOrderValue,
        TotalCommissionAmount = totalCommissionAmount,
        TotalNetAmount = totalNetAmount
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerReportResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerReportResult Failure(string error) => Failure([error]);
}
