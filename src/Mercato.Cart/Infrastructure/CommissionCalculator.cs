using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Microsoft.Extensions.Logging;

namespace Mercato.Cart.Infrastructure;

/// <summary>
/// Service implementation for calculating internal platform commissions.
/// Commission calculations are for internal use only and not visible to buyers.
/// </summary>
public class CommissionCalculator : ICommissionCalculator
{
    private readonly ILogger<CommissionCalculator> _logger;

    /// <summary>
    /// Default commission rate for all stores (10%).
    /// In a production system, this could be configurable per seller tier or category.
    /// </summary>
    private const decimal DefaultCommissionRate = 0.10m;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionCalculator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public CommissionCalculator(ILogger<CommissionCalculator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, StoreCommission>> CalculateCommissionsAsync(
        CartTotals cartTotals,
        IReadOnlyList<CartItemsByStore> itemsByStore)
    {
        var result = new Dictionary<Guid, StoreCommission>();

        foreach (var storeGroup in itemsByStore)
        {
            var itemSubtotal = storeGroup.Subtotal;
            var shippingCost = cartTotals.ShippingByStore.TryGetValue(storeGroup.StoreId, out var shipping)
                ? shipping.ShippingCost
                : 0;

            var grossAmount = itemSubtotal + shippingCost;
            var commissionRate = DefaultCommissionRate;
            var commissionAmount = Math.Round(grossAmount * commissionRate, 2);
            var netPayout = grossAmount - commissionAmount;

            result[storeGroup.StoreId] = new StoreCommission
            {
                StoreId = storeGroup.StoreId,
                StoreName = storeGroup.StoreName,
                GrossAmount = grossAmount,
                CommissionAmount = commissionAmount,
                CommissionRate = commissionRate,
                NetPayout = netPayout
            };
        }

        _logger.LogInformation(
            "Calculated commissions for {StoreCount} stores, total commission: {TotalCommission}",
            result.Count,
            result.Values.Sum(c => c.CommissionAmount));

        return Task.FromResult<IReadOnlyDictionary<Guid, StoreCommission>>(result);
    }
}
