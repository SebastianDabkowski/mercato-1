using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Domain.Interfaces;

/// <summary>
/// Repository interface for store shipping provider configuration data access operations.
/// </summary>
public interface IStoreShippingProviderRepository
{
    /// <summary>
    /// Gets a store shipping provider configuration by its unique identifier.
    /// </summary>
    /// <param name="id">The store shipping provider identifier.</param>
    /// <returns>The store shipping provider if found; otherwise, null.</returns>
    Task<StoreShippingProvider?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all shipping providers enabled for a specific store.
    /// </summary>
    /// <param name="storeId">The store identifier.</param>
    /// <returns>A read-only list of store shipping providers for the store.</returns>
    Task<IReadOnlyList<StoreShippingProvider>> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets all enabled shipping providers for a specific store.
    /// </summary>
    /// <param name="storeId">The store identifier.</param>
    /// <returns>A read-only list of enabled store shipping providers for the store.</returns>
    Task<IReadOnlyList<StoreShippingProvider>> GetEnabledByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets a specific store's configuration for a shipping provider.
    /// </summary>
    /// <param name="storeId">The store identifier.</param>
    /// <param name="shippingProviderId">The shipping provider identifier.</param>
    /// <returns>The store shipping provider configuration if found; otherwise, null.</returns>
    Task<StoreShippingProvider?> GetByStoreAndProviderAsync(Guid storeId, Guid shippingProviderId);

    /// <summary>
    /// Adds a new store shipping provider configuration.
    /// </summary>
    /// <param name="storeShippingProvider">The store shipping provider configuration to add.</param>
    /// <returns>The added store shipping provider configuration.</returns>
    Task<StoreShippingProvider> AddAsync(StoreShippingProvider storeShippingProvider);

    /// <summary>
    /// Updates an existing store shipping provider configuration.
    /// </summary>
    /// <param name="storeShippingProvider">The store shipping provider configuration to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(StoreShippingProvider storeShippingProvider);

    /// <summary>
    /// Deletes a store shipping provider configuration.
    /// </summary>
    /// <param name="id">The store shipping provider identifier to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);
}
