using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for generating commission summary reports with CSV export.
/// </summary>
public interface ICommissionSummaryService
{
    /// <summary>
    /// Gets the commission summary grouped by seller based on the provided filter criteria.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the summary data grouped by seller.</returns>
    Task<GetCommissionSummaryResult> GetSummaryAsync(CommissionSummaryFilterQuery query);

    /// <summary>
    /// Gets the individual orders for a specific seller within a date range.
    /// </summary>
    /// <param name="sellerId">The seller's unique identifier.</param>
    /// <param name="fromDate">The start date for date range filter (optional, inclusive).</param>
    /// <param name="toDate">The end date for date range filter (optional, inclusive).</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <returns>The result containing the order rows for the seller.</returns>
    Task<GetSellerOrdersResult> GetSellerOrdersAsync(
        Guid sellerId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize);

    /// <summary>
    /// Exports the commission summary to CSV format.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the CSV content as bytes.</returns>
    Task<ExportCommissionSummaryResult> ExportToCsvAsync(CommissionSummaryFilterQuery query);
}
