using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for generating order and revenue reports with CSV export.
/// </summary>
public interface IOrderRevenueReportService
{
    /// <summary>
    /// Gets the order and revenue report based on the provided filter criteria.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the report data.</returns>
    Task<GetOrderRevenueReportResult> GetReportAsync(OrderRevenueReportFilterQuery query);

    /// <summary>
    /// Exports the order and revenue report to CSV format.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the CSV content as bytes.</returns>
    Task<ExportOrderRevenueReportResult> ExportToCsvAsync(OrderRevenueReportFilterQuery query);

    /// <summary>
    /// Gets the list of distinct sellers for the filter dropdown.
    /// </summary>
    /// <returns>A list of tuples containing seller ID and seller name.</returns>
    Task<IReadOnlyList<(Guid SellerId, string SellerName)>> GetDistinctSellersAsync();
}
