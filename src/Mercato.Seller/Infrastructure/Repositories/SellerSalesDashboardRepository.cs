using Mercato.Orders.Infrastructure.Persistence;
using Mercato.Seller.Application.Queries;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for seller sales dashboard data aggregations.
/// </summary>
public class SellerSalesDashboardRepository : ISellerSalesDashboardRepository
{
    private readonly OrderDbContext _orderDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerSalesDashboardRepository"/> class.
    /// </summary>
    /// <param name="orderDbContext">The orders database context.</param>
    public SellerSalesDashboardRepository(OrderDbContext orderDbContext)
    {
        ArgumentNullException.ThrowIfNull(orderDbContext);
        _orderDbContext = orderDbContext;
    }

    /// <inheritdoc/>
    public async Task<(int OrderCount, decimal TotalGmv)> GetSalesMetricsAsync(
        Guid storeId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid? productId = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _orderDbContext.SellerSubOrders
            .Where(so => so.StoreId == storeId &&
                         so.CreatedAt >= startDate &&
                         so.CreatedAt <= endDate);

        // Apply product/category filters if specified
        if (productId.HasValue || !string.IsNullOrEmpty(category))
        {
            query = ApplyItemFilters(query, productId, category);
        }

        var metrics = await query
            .GroupBy(so => 1)
            .Select(g => new
            {
                OrderCount = g.Count(),
                TotalGmv = g.Sum(so => so.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return metrics != null ? (metrics.OrderCount, metrics.TotalGmv) : (0, 0m);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SalesChartDataPoint>> GetSalesChartDataAsync(
        Guid storeId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        SalesGranularity granularity,
        Guid? productId = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _orderDbContext.SellerSubOrders
            .Where(so => so.StoreId == storeId &&
                         so.CreatedAt >= startDate &&
                         so.CreatedAt <= endDate);

        // Apply product/category filters if specified
        if (productId.HasValue || !string.IsNullOrEmpty(category))
        {
            query = ApplyItemFilters(query, productId, category);
        }

        return granularity switch
        {
            SalesGranularity.Day => await GetDailyChartDataAsync(query, cancellationToken),
            SalesGranularity.Week => await GetWeeklyChartDataAsync(query, cancellationToken),
            SalesGranularity.Month => await GetMonthlyChartDataAsync(query, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity))
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProductFilterItem>> GetProductsForFilterAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        // Get distinct products from seller's sub-order items
        var products = await _orderDbContext.SellerSubOrderItems
            .Where(soi => soi.SellerSubOrder.StoreId == storeId)
            .Select(soi => new { soi.ProductId, soi.ProductTitle })
            .Distinct()
            .OrderBy(p => p.ProductTitle)
            .ToListAsync(cancellationToken);

        return products.Select(p => new ProductFilterItem
        {
            Id = p.ProductId,
            Title = p.ProductTitle
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetCategoriesForFilterAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Clarify how to obtain category information for products.
        // SellerSubOrderItem does not currently store category information.
        // Options: join with Product entity, add Category to SellerSubOrderItem, or use a separate lookup.
        // For now, return an empty list until the category data source is clarified.
        return await Task.FromResult<IReadOnlyList<string>>([]);
    }

    private static IQueryable<Orders.Domain.Entities.SellerSubOrder> ApplyItemFilters(
        IQueryable<Orders.Domain.Entities.SellerSubOrder> query,
        Guid? productId,
        string? category)
    {
        if (productId.HasValue)
        {
            query = query.Where(so => so.Items.Any(i => i.ProductId == productId.Value));
        }

        // TODO: Clarify category filter implementation.
        // Category is not stored in SellerSubOrderItem. This filter is ignored until clarified.
        // if (!string.IsNullOrEmpty(category))
        // {
        //     query = query.Where(so => so.Items.Any(i => i.Category == category));
        // }

        return query;
    }

    private static async Task<IReadOnlyList<SalesChartDataPoint>> GetDailyChartDataAsync(
        IQueryable<Orders.Domain.Entities.SellerSubOrder> query,
        CancellationToken cancellationToken)
    {
        var data = await query
            .GroupBy(so => new { so.CreatedAt.Year, so.CreatedAt.Month, so.CreatedAt.Day })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                Gmv = g.Sum(so => so.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
            .ToListAsync(cancellationToken);

        return data.Select(x => new SalesChartDataPoint
        {
            Date = new DateTimeOffset(new DateTime(x.Year, x.Month, x.Day), TimeSpan.Zero),
            Gmv = x.Gmv,
            OrderCount = x.OrderCount
        }).ToList();
    }

    private static async Task<IReadOnlyList<SalesChartDataPoint>> GetWeeklyChartDataAsync(
        IQueryable<Orders.Domain.Entities.SellerSubOrder> query,
        CancellationToken cancellationToken)
    {
        // Use a fixed reference date for consistent week calculation across years
        var referenceDate = new DateTimeOffset(new DateTime(2000, 1, 3), TimeSpan.Zero); // Monday, Jan 3, 2000
        
        var data = await query
            .GroupBy(so => EF.Functions.DateDiffWeek(referenceDate, so.CreatedAt))
            .Select(g => new
            {
                WeekNumber = g.Key,
                Gmv = g.Sum(so => so.TotalAmount),
                OrderCount = g.Count(),
                // Get the earliest date in this week group for display
                FirstOrderDate = g.Min(so => so.CreatedAt)
            })
            .OrderBy(x => x.WeekNumber)
            .ToListAsync(cancellationToken);

        return data.Select(x =>
        {
            // Calculate the Monday of the week for this data point
            var weekStart = referenceDate.AddDays(x.WeekNumber * 7);
            return new SalesChartDataPoint
            {
                Date = weekStart,
                Gmv = x.Gmv,
                OrderCount = x.OrderCount
            };
        }).ToList();
    }

    private static async Task<IReadOnlyList<SalesChartDataPoint>> GetMonthlyChartDataAsync(
        IQueryable<Orders.Domain.Entities.SellerSubOrder> query,
        CancellationToken cancellationToken)
    {
        var data = await query
            .GroupBy(so => new { so.CreatedAt.Year, so.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Gmv = g.Sum(so => so.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return data.Select(x => new SalesChartDataPoint
        {
            Date = new DateTimeOffset(new DateTime(x.Year, x.Month, 1), TimeSpan.Zero),
            Gmv = x.Gmv,
            OrderCount = x.OrderCount
        }).ToList();
    }
}
