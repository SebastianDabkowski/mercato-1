using Mercato.Seller.Application.Queries;

namespace Mercato.Seller.Application.Services;

/// <summary>
/// Service interface for seller sales dashboard operations.
/// </summary>
public interface ISellerSalesDashboardService
{
    /// <summary>
    /// Gets seller sales dashboard data for the specified query parameters.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard result with sales metrics and chart data.</returns>
    Task<SellerSalesDashboardResult> GetDashboardAsync(
        SellerSalesDashboardQuery query,
        CancellationToken cancellationToken = default);
}
