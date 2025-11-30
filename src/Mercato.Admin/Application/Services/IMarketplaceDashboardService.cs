using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for marketplace dashboard operations.
/// </summary>
public interface IMarketplaceDashboardService
{
    /// <summary>
    /// Gets marketplace performance metrics for the specified period.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard result with KPIs.</returns>
    Task<MarketplaceDashboardResult> GetDashboardAsync(MarketplaceDashboardQuery query, CancellationToken cancellationToken = default);
}
