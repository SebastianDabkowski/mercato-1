using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Interfaces;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for marketplace dashboard operations.
/// </summary>
public class MarketplaceDashboardService : IMarketplaceDashboardService
{
    private readonly IMarketplaceDashboardRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketplaceDashboardService"/> class.
    /// </summary>
    /// <param name="repository">The marketplace dashboard repository.</param>
    public MarketplaceDashboardService(IMarketplaceDashboardRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<MarketplaceDashboardResult> GetDashboardAsync(MarketplaceDashboardQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var orderMetricsTask = _repository.GetOrderMetricsAsync(query.StartDate, query.EndDate, cancellationToken);
        var activeSellerCountTask = _repository.GetActiveSellerCountAsync(cancellationToken);
        var activeProductCountTask = _repository.GetActiveProductCountAsync(cancellationToken);
        var newUserCountTask = _repository.GetNewUserCountAsync(query.StartDate, query.EndDate, cancellationToken);

        await Task.WhenAll(orderMetricsTask, activeSellerCountTask, activeProductCountTask, newUserCountTask);

        var orderMetrics = await orderMetricsTask;

        return new MarketplaceDashboardResult
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            TotalGmv = orderMetrics.TotalGmv,
            TotalOrders = orderMetrics.OrderCount,
            ActiveSellers = await activeSellerCountTask,
            ActiveProducts = await activeProductCountTask,
            NewUsers = await newUserCountTask,
            RetrievedAt = DateTimeOffset.UtcNow
        };
    }
}
