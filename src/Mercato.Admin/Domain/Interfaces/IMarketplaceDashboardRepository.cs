namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for marketplace dashboard data aggregations.
/// </summary>
public interface IMarketplaceDashboardRepository
{
    /// <summary>
    /// Gets the count of orders and total GMV for the specified period.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple with order count and total GMV.</returns>
    Task<(int OrderCount, decimal TotalGmv)> GetOrderMetricsAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active sellers (stores with Active or LimitedActive status).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of active sellers.</returns>
    Task<int> GetActiveSellerCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active products (products with Active status).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of active products.</returns>
    Task<int> GetActiveProductCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of newly registered users in the specified period.
    /// </summary>
    /// <param name="startDate">The start date of the period.</param>
    /// <param name="endDate">The end date of the period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of new users.</returns>
    Task<int> GetNewUserCountAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
}
