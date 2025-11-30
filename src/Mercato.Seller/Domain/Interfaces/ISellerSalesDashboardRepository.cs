using Mercato.Seller.Application.Queries;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for seller sales dashboard data aggregations.
/// </summary>
public interface ISellerSalesDashboardRepository
{
    /// <summary>
    /// Gets the count of sub-orders and total GMV for the specified seller and period.
    /// </summary>
    /// <param name="storeId">The store ID to filter by.</param>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="productId">Optional product ID filter.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple with order count and total GMV.</returns>
    Task<(int OrderCount, decimal TotalGmv)> GetSalesMetricsAsync(
        Guid storeId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid? productId = null,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sales chart data aggregated by the specified granularity.
    /// </summary>
    /// <param name="storeId">The store ID to filter by.</param>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="granularity">The time granularity for aggregation.</param>
    /// <param name="productId">Optional product ID filter.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chart data points.</returns>
    Task<IReadOnlyList<SalesChartDataPoint>> GetSalesChartDataAsync(
        Guid storeId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        SalesGranularity granularity,
        Guid? productId = null,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of products available for filtering for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of products with their IDs and titles.</returns>
    Task<IReadOnlyList<ProductFilterItem>> GetProductsForFilterAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of categories available for filtering for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of category names.</returns>
    Task<IReadOnlyList<string>> GetCategoriesForFilterAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);
}
