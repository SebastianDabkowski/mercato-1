using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for shipping rule operations.
/// </summary>
public interface IShippingRuleRepository
{
    /// <summary>
    /// Gets the shipping rule for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The shipping rule if found; otherwise, null.</returns>
    Task<ShippingRule?> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets shipping rules for multiple stores.
    /// </summary>
    /// <param name="storeIds">The store IDs.</param>
    /// <returns>A dictionary of store IDs to their shipping rules.</returns>
    Task<IDictionary<Guid, ShippingRule>> GetByStoreIdsAsync(IEnumerable<Guid> storeIds);
}
