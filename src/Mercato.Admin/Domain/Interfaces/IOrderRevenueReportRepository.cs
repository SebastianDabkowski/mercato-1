using Mercato.Admin.Application.Queries;
using Mercato.Orders.Domain.Entities;
using Mercato.Payments.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for aggregating order and revenue report data.
/// </summary>
public interface IOrderRevenueReportRepository
{
    /// <summary>
    /// Gets the report data based on the provided filter criteria.
    /// </summary>
    /// <param name="fromDate">The start date for the date range filter (inclusive).</param>
    /// <param name="toDate">The end date for the date range filter (inclusive).</param>
    /// <param name="sellerId">The seller ID to filter by.</param>
    /// <param name="orderStatuses">The order statuses to filter by.</param>
    /// <param name="paymentStatuses">The payment statuses to filter by.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <returns>
    /// A tuple containing the report rows, total count, total order value, total commission, and total payout amount.
    /// </returns>
    Task<(IReadOnlyList<OrderRevenueReportRow> Rows, int TotalCount, decimal TotalOrderValue, decimal TotalCommission, decimal TotalPayoutAmount)> GetReportDataAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        Guid? sellerId,
        IReadOnlyList<OrderStatus>? orderStatuses,
        IReadOnlyList<PaymentStatus>? paymentStatuses,
        int page,
        int pageSize);

    /// <summary>
    /// Gets the total count of matching rows without fetching all data.
    /// </summary>
    /// <param name="fromDate">The start date for the date range filter (inclusive).</param>
    /// <param name="toDate">The end date for the date range filter (inclusive).</param>
    /// <param name="sellerId">The seller ID to filter by.</param>
    /// <param name="orderStatuses">The order statuses to filter by.</param>
    /// <param name="paymentStatuses">The payment statuses to filter by.</param>
    /// <returns>The count of matching rows.</returns>
    Task<int> GetCountAsync(
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        Guid? sellerId,
        IReadOnlyList<OrderStatus>? orderStatuses,
        IReadOnlyList<PaymentStatus>? paymentStatuses);

    /// <summary>
    /// Gets the list of distinct sellers for the filter dropdown.
    /// </summary>
    /// <returns>A list of tuples containing seller ID and seller name.</returns>
    Task<IReadOnlyList<(Guid SellerId, string SellerName)>> GetDistinctSellersAsync();
}
