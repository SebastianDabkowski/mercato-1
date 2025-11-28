using Mercato.Buyer.Application.Queries;

namespace Mercato.Buyer.Application.Services;

/// <summary>
/// Service interface for recently viewed products operations.
/// </summary>
public interface IRecentlyViewedService
{
    /// <summary>
    /// Gets recently viewed products for the provided product IDs.
    /// Only returns active, visible products that still exist.
    /// </summary>
    /// <param name="productIds">The list of product IDs in order from most recent to oldest.</param>
    /// <param name="maxItems">The maximum number of items to return.</param>
    /// <returns>The result containing valid recently viewed products.</returns>
    Task<RecentlyViewedProductsResult> GetRecentlyViewedProductsAsync(IEnumerable<Guid> productIds, int maxItems = 10);
}
