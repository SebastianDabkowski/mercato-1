using Mercato.Seller.Application.Queries;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Interfaces;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Service implementation for seller sales dashboard operations.
/// </summary>
public class SellerSalesDashboardService : ISellerSalesDashboardService
{
    private readonly ISellerSalesDashboardRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerSalesDashboardService"/> class.
    /// </summary>
    /// <param name="repository">The seller sales dashboard repository.</param>
    public SellerSalesDashboardService(ISellerSalesDashboardRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<SellerSalesDashboardResult> GetDashboardAsync(
        SellerSalesDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Fetch all data in parallel for better performance
        var metricsTask = _repository.GetSalesMetricsAsync(
            query.StoreId,
            query.StartDate,
            query.EndDate,
            query.ProductId,
            query.Category,
            cancellationToken);

        var chartDataTask = _repository.GetSalesChartDataAsync(
            query.StoreId,
            query.StartDate,
            query.EndDate,
            query.Granularity,
            query.ProductId,
            query.Category,
            cancellationToken);

        var productsTask = _repository.GetProductsForFilterAsync(query.StoreId, cancellationToken);
        var categoriesTask = _repository.GetCategoriesForFilterAsync(query.StoreId, cancellationToken);

        await Task.WhenAll(metricsTask, chartDataTask, productsTask, categoriesTask);

        var metrics = await metricsTask;
        var chartData = await chartDataTask;
        var products = await productsTask;
        var categories = await categoriesTask;

        return new SellerSalesDashboardResult
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            TotalGmv = metrics.TotalGmv,
            TotalOrders = metrics.OrderCount,
            ChartDataPoints = chartData,
            AvailableProducts = products,
            AvailableCategories = categories,
            RetrievedAt = DateTimeOffset.UtcNow
        };
    }
}
