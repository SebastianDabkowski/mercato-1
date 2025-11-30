using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Domain.Entities;

namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query parameters for filtering order and revenue report data.
/// </summary>
public class OrderRevenueReportFilterQuery
{
    /// <summary>
    /// Gets or sets the start date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Gets or sets the seller ID to filter by (optional).
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the order statuses to filter by (optional). If empty, all statuses are included.
    /// </summary>
    public IReadOnlyList<OrderStatus> OrderStatuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the payment statuses to filter by (optional). If empty, all statuses are included.
    /// </summary>
    public IReadOnlyList<PaymentStatus> PaymentStatuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Represents a single row in the order and revenue report.
/// </summary>
public class OrderRevenueReportRow
{
    /// <summary>
    /// Gets or sets the unique identifier of the order.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the order was created.
    /// </summary>
    public DateTimeOffset OrderDate { get; set; }

    /// <summary>
    /// Gets or sets the buyer's email address or ID if email is not available.
    /// </summary>
    public string BuyerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller's store name.
    /// </summary>
    public string SellerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller's unique identifier.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    public OrderStatus OrderStatus { get; set; }

    /// <summary>
    /// Gets or sets the payment status of the order.
    /// </summary>
    public PaymentStatus PaymentStatus { get; set; }

    /// <summary>
    /// Gets or sets the total order value.
    /// </summary>
    public decimal OrderValue { get; set; }

    /// <summary>
    /// Gets or sets the commission amount for the marketplace.
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Gets or sets the payout amount to the seller (OrderValue - Commission).
    /// </summary>
    public decimal PayoutAmount { get; set; }
}

/// <summary>
/// Result of the order and revenue report query.
/// </summary>
public class GetOrderRevenueReportResult
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
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the report rows for the current page.
    /// </summary>
    public IReadOnlyList<OrderRevenueReportRow> Rows { get; private init; } = [];

    /// <summary>
    /// Gets the total number of rows matching the filter criteria.
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
    /// Gets the total order value across all matching rows.
    /// </summary>
    public decimal TotalOrderValue { get; private init; }

    /// <summary>
    /// Gets the total commission across all matching rows.
    /// </summary>
    public decimal TotalCommission { get; private init; }

    /// <summary>
    /// Gets the total payout amount across all matching rows.
    /// </summary>
    public decimal TotalPayoutAmount { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="rows">The report rows for the current page.</param>
    /// <param name="totalCount">The total number of rows matching the filter.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="totalOrderValue">The total order value across all matching rows.</param>
    /// <param name="totalCommission">The total commission across all matching rows.</param>
    /// <param name="totalPayoutAmount">The total payout amount across all matching rows.</param>
    /// <returns>A successful result.</returns>
    public static GetOrderRevenueReportResult Success(
        IReadOnlyList<OrderRevenueReportRow> rows,
        int totalCount,
        int page,
        int pageSize,
        decimal totalOrderValue,
        decimal totalCommission,
        decimal totalPayoutAmount) => new()
    {
        Succeeded = true,
        Errors = [],
        Rows = rows,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalOrderValue = totalOrderValue,
        TotalCommission = totalCommission,
        TotalPayoutAmount = totalPayoutAmount
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetOrderRevenueReportResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetOrderRevenueReportResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetOrderRevenueReportResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access the report."]
    };
}

/// <summary>
/// Result for the CSV export operation.
/// </summary>
public class ExportOrderRevenueReportResult
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
    /// Gets the CSV file content as bytes.
    /// </summary>
    public byte[] CsvContent { get; private init; } = [];

    /// <summary>
    /// Gets the suggested filename for the CSV export.
    /// </summary>
    public string FileName { get; private init; } = string.Empty;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="csvContent">The CSV file content as bytes.</param>
    /// <param name="fileName">The suggested filename for the CSV export.</param>
    /// <returns>A successful result.</returns>
    public static ExportOrderRevenueReportResult Success(byte[] csvContent, string fileName) => new()
    {
        Succeeded = true,
        Errors = [],
        CsvContent = csvContent,
        FileName = fileName
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static ExportOrderRevenueReportResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ExportOrderRevenueReportResult Failure(string error) => Failure([error]);
}
