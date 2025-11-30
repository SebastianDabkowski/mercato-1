using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for aggregating commission summary data.
/// </summary>
public interface ICommissionSummaryRepository
{
    /// <summary>
    /// Gets the commission summary data grouped by seller based on the provided filter criteria.
    /// </summary>
    /// <param name="fromDate">The start date for the date range filter (inclusive).</param>
    /// <param name="toDate">The end date for the date range filter (inclusive).</param>
    /// <returns>
    /// A tuple containing the summary rows grouped by seller, total GMV, total commission, total net payout, and total order count.
    /// </returns>
    Task<(IReadOnlyList<SellerCommissionSummaryRow> Rows, decimal TotalGMV, decimal TotalCommission, decimal TotalNetPayout, int TotalOrderCount)> GetSummaryDataAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate);

    /// <summary>
    /// Gets the individual orders for a specific seller within a date range.
    /// </summary>
    /// <param name="sellerId">The seller's unique identifier.</param>
    /// <param name="fromDate">The start date for the date range filter (inclusive).</param>
    /// <param name="toDate">The end date for the date range filter (inclusive).</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <returns>
    /// A tuple containing the order rows, total count, seller ID, and seller name.
    /// </returns>
    Task<(IReadOnlyList<OrderCommissionRow> Rows, int TotalCount, Guid SellerId, string SellerName)> GetSellerOrdersAsync(
        Guid sellerId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize);
}
