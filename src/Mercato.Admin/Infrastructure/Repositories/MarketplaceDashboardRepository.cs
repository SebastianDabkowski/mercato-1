using Mercato.Admin.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Infrastructure.Persistence;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for marketplace dashboard data aggregations.
/// </summary>
public class MarketplaceDashboardRepository : IMarketplaceDashboardRepository
{
    private readonly OrderDbContext _orderDbContext;
    private readonly SellerDbContext _sellerDbContext;
    private readonly ProductDbContext _productDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarketplaceDashboardRepository"/> class.
    /// </summary>
    /// <param name="orderDbContext">The orders database context.</param>
    /// <param name="sellerDbContext">The seller database context.</param>
    /// <param name="productDbContext">The product database context.</param>
    public MarketplaceDashboardRepository(
        OrderDbContext orderDbContext,
        SellerDbContext sellerDbContext,
        ProductDbContext productDbContext)
    {
        ArgumentNullException.ThrowIfNull(orderDbContext);
        ArgumentNullException.ThrowIfNull(sellerDbContext);
        ArgumentNullException.ThrowIfNull(productDbContext);

        _orderDbContext = orderDbContext;
        _sellerDbContext = sellerDbContext;
        _productDbContext = productDbContext;
    }

    /// <inheritdoc/>
    public async Task<(int OrderCount, decimal TotalGmv)> GetOrderMetricsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var metrics = await _orderDbContext.Orders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .GroupBy(o => 1)
            .Select(g => new
            {
                OrderCount = g.Count(),
                TotalGmv = g.Sum(o => o.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return metrics != null ? (metrics.OrderCount, metrics.TotalGmv) : (0, 0m);
    }

    /// <inheritdoc/>
    public async Task<int> GetActiveSellerCountAsync(CancellationToken cancellationToken = default)
    {
        return await _sellerDbContext.Stores
            .Where(s => s.Status == StoreStatus.Active || s.Status == StoreStatus.LimitedActive)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetActiveProductCountAsync(CancellationToken cancellationToken = default)
    {
        return await _productDbContext.Products
            .Where(p => p.Status == ProductStatus.Active)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> GetNewUserCountAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        // TODO: Clarify how to track user registration date. The default IdentityUser does not have a CreatedAt field.
        // Consider extending IdentityUser with a RegistrationDate property or using a separate registration audit table.
        // For now, this returns 0 since we cannot determine when users were registered.
        return Task.FromResult(0);
    }
}
