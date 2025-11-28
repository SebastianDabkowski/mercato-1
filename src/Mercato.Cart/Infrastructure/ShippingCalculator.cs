using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Cart.Infrastructure;

/// <summary>
/// Service implementation for calculating shipping costs based on cart contents.
/// </summary>
public class ShippingCalculator : IShippingCalculator
{
    private readonly IShippingRuleRepository _shippingRuleRepository;
    private readonly ILogger<ShippingCalculator> _logger;

    /// <summary>
    /// Default flat rate shipping cost when no rule is configured.
    /// </summary>
    private const decimal DefaultFlatRate = 5.99m;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingCalculator"/> class.
    /// </summary>
    /// <param name="shippingRuleRepository">The shipping rule repository.</param>
    /// <param name="logger">The logger.</param>
    public ShippingCalculator(
        IShippingRuleRepository shippingRuleRepository,
        ILogger<ShippingCalculator> logger)
    {
        _shippingRuleRepository = shippingRuleRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, StoreShippingCost>> CalculateShippingAsync(
        IReadOnlyList<CartItemsByStore> itemsByStore)
    {
        if (itemsByStore.Count == 0)
        {
            return new Dictionary<Guid, StoreShippingCost>();
        }

        var storeIds = itemsByStore.Select(g => g.StoreId).ToList();
        var shippingRules = await _shippingRuleRepository.GetByStoreIdsAsync(storeIds);

        var result = new Dictionary<Guid, StoreShippingCost>();

        foreach (var storeGroup in itemsByStore)
        {
            var shippingCost = CalculateStoreShipping(storeGroup, shippingRules);
            result[storeGroup.StoreId] = shippingCost;
        }

        _logger.LogInformation(
            "Calculated shipping for {StoreCount} stores, total shipping: {TotalShipping}",
            result.Count,
            result.Values.Sum(s => s.ShippingCost));

        return result;
    }

    private static StoreShippingCost CalculateStoreShipping(
        CartItemsByStore storeGroup,
        IDictionary<Guid, Seller.Domain.Entities.ShippingRule> shippingRules)
    {
        var subtotal = storeGroup.Subtotal;
        var itemCount = storeGroup.Items.Sum(i => i.Quantity);

        if (!shippingRules.TryGetValue(storeGroup.StoreId, out var rule))
        {
            // No shipping rule configured, use default flat rate
            return new StoreShippingCost
            {
                StoreId = storeGroup.StoreId,
                StoreName = storeGroup.StoreName,
                ShippingCost = DefaultFlatRate,
                IsFreeShipping = false,
                AmountToFreeShipping = null
            };
        }

        // Check free shipping threshold
        if (rule.FreeShippingThreshold.HasValue && subtotal >= rule.FreeShippingThreshold.Value)
        {
            return new StoreShippingCost
            {
                StoreId = storeGroup.StoreId,
                StoreName = storeGroup.StoreName,
                ShippingCost = 0,
                IsFreeShipping = true,
                AmountToFreeShipping = null
            };
        }

        // Calculate shipping cost: flat rate + (per item rate × item count)
        var shippingCost = rule.FlatRate + (rule.PerItemRate * itemCount);

        // Calculate amount needed for free shipping
        decimal? amountToFreeShipping = null;
        if (rule.FreeShippingThreshold.HasValue)
        {
            amountToFreeShipping = rule.FreeShippingThreshold.Value - subtotal;
        }

        return new StoreShippingCost
        {
            StoreId = storeGroup.StoreId,
            StoreName = storeGroup.StoreName,
            ShippingCost = shippingCost,
            IsFreeShipping = false,
            AmountToFreeShipping = amountToFreeShipping
        };
    }
}
