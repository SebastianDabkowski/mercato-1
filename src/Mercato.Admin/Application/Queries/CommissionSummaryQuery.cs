namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query parameters for filtering commission summary report data.
/// </summary>
public class CommissionSummaryFilterQuery
{
    /// <summary>
    /// Gets or sets the start date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (optional, inclusive).
    /// </summary>
    public DateTimeOffset? ToDate { get; set; }
}

/// <summary>
/// Represents a single row in the commission summary grouped by seller.
/// </summary>
public class SellerCommissionSummaryRow
{
    /// <summary>
    /// Gets or sets the seller's unique identifier.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the seller's store name.
    /// </summary>
    public string SellerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total Gross Merchandise Value (GMV) for the seller.
    /// </summary>
    public decimal TotalGMV { get; set; }

    /// <summary>
    /// Gets or sets the total commission amount for the seller.
    /// </summary>
    public decimal TotalCommission { get; set; }

    /// <summary>
    /// Gets or sets the total net payout to the seller (GMV - Commission).
    /// </summary>
    public decimal TotalNetPayout { get; set; }

    /// <summary>
    /// Gets or sets the count of orders for the seller.
    /// </summary>
    public int OrderCount { get; set; }
}

/// <summary>
/// Result of the commission summary query.
/// </summary>
public class GetCommissionSummaryResult
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
    /// Gets the summary rows grouped by seller.
    /// </summary>
    public IReadOnlyList<SellerCommissionSummaryRow> Rows { get; private init; } = [];

    /// <summary>
    /// Gets the total GMV across all sellers.
    /// </summary>
    public decimal TotalGMV { get; private init; }

    /// <summary>
    /// Gets the total commission across all sellers.
    /// </summary>
    public decimal TotalCommission { get; private init; }

    /// <summary>
    /// Gets the total net payout across all sellers.
    /// </summary>
    public decimal TotalNetPayout { get; private init; }

    /// <summary>
    /// Gets the total order count across all sellers.
    /// </summary>
    public int TotalOrderCount { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="rows">The summary rows grouped by seller.</param>
    /// <param name="totalGMV">The total GMV across all sellers.</param>
    /// <param name="totalCommission">The total commission across all sellers.</param>
    /// <param name="totalNetPayout">The total net payout across all sellers.</param>
    /// <param name="totalOrderCount">The total order count across all sellers.</param>
    /// <returns>A successful result.</returns>
    public static GetCommissionSummaryResult Success(
        IReadOnlyList<SellerCommissionSummaryRow> rows,
        decimal totalGMV,
        decimal totalCommission,
        decimal totalNetPayout,
        int totalOrderCount) => new()
    {
        Succeeded = true,
        Errors = [],
        Rows = rows,
        TotalGMV = totalGMV,
        TotalCommission = totalCommission,
        TotalNetPayout = totalNetPayout,
        TotalOrderCount = totalOrderCount
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetCommissionSummaryResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetCommissionSummaryResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetCommissionSummaryResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access the commission summary report."]
    };
}

/// <summary>
/// Represents a single order row in the seller orders drill-down.
/// </summary>
public class OrderCommissionRow
{
    /// <summary>
    /// Gets or sets the unique identifier of the order.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order date.
    /// </summary>
    public DateTimeOffset OrderDate { get; set; }

    /// <summary>
    /// Gets or sets the order amount (GMV).
    /// </summary>
    public decimal OrderAmount { get; set; }

    /// <summary>
    /// Gets or sets the commission rate applied.
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Gets or sets the commission amount.
    /// </summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the net payout amount (OrderAmount - CommissionAmount).
    /// </summary>
    public decimal NetPayout { get; set; }

    /// <summary>
    /// Gets or sets the date when the commission was calculated.
    /// </summary>
    public DateTimeOffset CalculatedAt { get; set; }
}

/// <summary>
/// Result of the seller orders drill-down query.
/// </summary>
public class GetSellerOrdersResult
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
    /// Gets the order rows for the seller.
    /// </summary>
    public IReadOnlyList<OrderCommissionRow> Rows { get; private init; } = [];

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
    /// Gets the seller's unique identifier.
    /// </summary>
    public Guid SellerId { get; private init; }

    /// <summary>
    /// Gets the seller's store name.
    /// </summary>
    public string SellerName { get; private init; } = string.Empty;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="rows">The order rows for the seller.</param>
    /// <param name="totalCount">The total number of rows matching the filter.</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="sellerId">The seller's unique identifier.</param>
    /// <param name="sellerName">The seller's store name.</param>
    /// <returns>A successful result.</returns>
    public static GetSellerOrdersResult Success(
        IReadOnlyList<OrderCommissionRow> rows,
        int totalCount,
        int page,
        int pageSize,
        Guid sellerId,
        string sellerName) => new()
    {
        Succeeded = true,
        Errors = [],
        Rows = rows,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        SellerId = sellerId,
        SellerName = sellerName
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerOrdersResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetSellerOrdersResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetSellerOrdersResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access the seller orders."]
    };
}

/// <summary>
/// Result for the CSV export operation.
/// </summary>
public class ExportCommissionSummaryResult
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
    public static ExportCommissionSummaryResult Success(byte[] csvContent, string fileName) => new()
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
    public static ExportCommissionSummaryResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ExportCommissionSummaryResult Failure(string error) => Failure([error]);
}
