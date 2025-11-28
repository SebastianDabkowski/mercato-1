using Mercato.Cart.Application.Queries;

namespace Mercato.Cart.Application.Services;

/// <summary>
/// Service interface for getting available shipping methods.
/// </summary>
public interface IShippingMethodService
{
    /// <summary>
    /// Gets available shipping methods for the specified stores.
    /// </summary>
    /// <param name="storeIds">The list of store IDs to get shipping methods for.</param>
    /// <param name="itemsByStore">The cart items grouped by store for cost calculation.</param>
    /// <returns>The result containing available shipping methods by store.</returns>
    Task<GetShippingMethodsResult> GetShippingMethodsAsync(
        IEnumerable<Guid> storeIds,
        IReadOnlyList<CartItemsByStore> itemsByStore);

    /// <summary>
    /// Validates the selected shipping methods for all stores.
    /// </summary>
    /// <param name="selectedMethods">Dictionary of store ID to selected shipping method ID.</param>
    /// <param name="storeIds">The list of store IDs that require shipping methods.</param>
    /// <returns>True if all selections are valid; otherwise, false.</returns>
    bool ValidateShippingMethodSelection(
        IReadOnlyDictionary<Guid, string> selectedMethods,
        IEnumerable<Guid> storeIds);

    /// <summary>
    /// Gets the total shipping cost for the selected methods.
    /// </summary>
    /// <param name="selectedMethods">Dictionary of store ID to selected shipping method ID.</param>
    /// <param name="itemsByStore">The cart items grouped by store for cost calculation.</param>
    /// <returns>The total shipping cost.</returns>
    Task<decimal> GetTotalShippingCostAsync(
        IReadOnlyDictionary<Guid, string> selectedMethods,
        IReadOnlyList<CartItemsByStore> itemsByStore);
}
