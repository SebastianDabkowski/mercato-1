using Mercato.Cart.Application.Queries;
using Mercato.Cart.Application.Services;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Cart.Infrastructure;

/// <summary>
/// Service implementation for getting available shipping methods based on store rules.
/// </summary>
public class ShippingMethodService : IShippingMethodService
{
    private readonly IShippingRuleRepository _shippingRuleRepository;
    private readonly ILogger<ShippingMethodService> _logger;

    /// <summary>
    /// Standard shipping method ID.
    /// </summary>
    private const string StandardMethodId = "standard";

    /// <summary>
    /// Express shipping method ID.
    /// </summary>
    private const string ExpressMethodId = "express";

    /// <summary>
    /// Default flat rate for standard shipping when no rule is configured.
    /// </summary>
    private const decimal DefaultStandardRate = 5.99m;

    /// <summary>
    /// Default flat rate for express shipping when no rule is configured.
    /// </summary>
    private const decimal DefaultExpressRate = 12.99m;

    /// <summary>
    /// Express shipping multiplier applied to the calculated rate.
    /// </summary>
    private const decimal ExpressMultiplier = 2.0m;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingMethodService"/> class.
    /// </summary>
    /// <param name="shippingRuleRepository">The shipping rule repository.</param>
    /// <param name="logger">The logger.</param>
    public ShippingMethodService(
        IShippingRuleRepository shippingRuleRepository,
        ILogger<ShippingMethodService> logger)
    {
        _shippingRuleRepository = shippingRuleRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetShippingMethodsResult> GetShippingMethodsAsync(
        IEnumerable<Guid> storeIds,
        IReadOnlyList<CartItemsByStore> itemsByStore)
    {
        var storeIdList = storeIds.ToList();
        if (storeIdList.Count == 0)
        {
            return GetShippingMethodsResult.Success(new Dictionary<Guid, IReadOnlyList<ShippingMethodDto>>());
        }

        var shippingRules = await _shippingRuleRepository.GetByStoreIdsAsync(storeIdList);
        var result = new Dictionary<Guid, IReadOnlyList<ShippingMethodDto>>();

        foreach (var storeGroup in itemsByStore)
        {
            var methods = CreateShippingMethodsForStore(storeGroup, shippingRules);
            result[storeGroup.StoreId] = methods;
        }

        _logger.LogInformation(
            "Retrieved shipping methods for {StoreCount} stores",
            result.Count);

        return GetShippingMethodsResult.Success(result);
    }

    /// <inheritdoc />
    public bool ValidateShippingMethodSelection(
        IReadOnlyDictionary<Guid, string> selectedMethods,
        IEnumerable<Guid> storeIds)
    {
        foreach (var storeId in storeIds)
        {
            if (!selectedMethods.TryGetValue(storeId, out var methodId))
            {
                _logger.LogWarning("No shipping method selected for store {StoreId}", storeId);
                return false;
            }

            if (methodId != StandardMethodId && methodId != ExpressMethodId)
            {
                _logger.LogWarning("Invalid shipping method {MethodId} for store {StoreId}", methodId, storeId);
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalShippingCostAsync(
        IReadOnlyDictionary<Guid, string> selectedMethods,
        IReadOnlyList<CartItemsByStore> itemsByStore)
    {
        var storeIds = itemsByStore.Select(g => g.StoreId).ToList();
        var shippingRules = await _shippingRuleRepository.GetByStoreIdsAsync(storeIds);

        decimal totalCost = 0;

        foreach (var storeGroup in itemsByStore)
        {
            if (!selectedMethods.TryGetValue(storeGroup.StoreId, out var methodId))
            {
                methodId = StandardMethodId;
            }

            var cost = CalculateShippingCost(storeGroup, shippingRules, methodId);
            totalCost += cost;
        }

        return totalCost;
    }

    private IReadOnlyList<ShippingMethodDto> CreateShippingMethodsForStore(
        CartItemsByStore storeGroup,
        IDictionary<Guid, Seller.Domain.Entities.ShippingRule> shippingRules)
    {
        var subtotal = storeGroup.Subtotal;
        var itemCount = storeGroup.Items.Sum(i => i.Quantity);

        shippingRules.TryGetValue(storeGroup.StoreId, out var rule);

        var standardCost = CalculateMethodCost(subtotal, itemCount, rule, isExpress: false);
        var expressCost = CalculateMethodCost(subtotal, itemCount, rule, isExpress: true);

        var methods = new List<ShippingMethodDto>
        {
            new ShippingMethodDto
            {
                Id = StandardMethodId,
                StoreId = storeGroup.StoreId,
                StoreName = storeGroup.StoreName,
                Name = "Standard Shipping",
                Description = "Delivery in 5-7 business days",
                Cost = standardCost,
                EstimatedDelivery = "5-7 business days",
                IsDefault = true
            },
            new ShippingMethodDto
            {
                Id = ExpressMethodId,
                StoreId = storeGroup.StoreId,
                StoreName = storeGroup.StoreName,
                Name = "Express Shipping",
                Description = "Delivery in 2-3 business days",
                Cost = expressCost,
                EstimatedDelivery = "2-3 business days",
                IsDefault = false
            }
        };

        return methods;
    }

    private static decimal CalculateMethodCost(
        decimal subtotal,
        int itemCount,
        Seller.Domain.Entities.ShippingRule? rule,
        bool isExpress)
    {
        decimal baseCost;

        if (rule == null)
        {
            baseCost = isExpress ? DefaultExpressRate : DefaultStandardRate;
        }
        else
        {
            // Check free shipping threshold (only for standard)
            if (!isExpress && rule.FreeShippingThreshold.HasValue && subtotal >= rule.FreeShippingThreshold.Value)
            {
                return 0;
            }

            baseCost = rule.FlatRate + (rule.PerItemRate * itemCount);
            if (isExpress)
            {
                baseCost *= ExpressMultiplier;
            }
        }

        return baseCost;
    }

    private decimal CalculateShippingCost(
        CartItemsByStore storeGroup,
        IDictionary<Guid, Seller.Domain.Entities.ShippingRule> shippingRules,
        string methodId)
    {
        var subtotal = storeGroup.Subtotal;
        var itemCount = storeGroup.Items.Sum(i => i.Quantity);

        shippingRules.TryGetValue(storeGroup.StoreId, out var rule);

        var isExpress = methodId == ExpressMethodId;
        return CalculateMethodCost(subtotal, itemCount, rule, isExpress);
    }
}
