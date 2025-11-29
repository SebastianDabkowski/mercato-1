using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for shipping method operations.
/// </summary>
public interface IShippingMethodRepository
{
    /// <summary>
    /// Gets a shipping method by its unique identifier.
    /// </summary>
    /// <param name="id">The shipping method ID.</param>
    /// <returns>The shipping method if found; otherwise, null.</returns>
    Task<ShippingMethod?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all shipping methods for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of all shipping methods for the store.</returns>
    Task<IReadOnlyList<ShippingMethod>> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets only active shipping methods for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of active shipping methods for the store.</returns>
    Task<IReadOnlyList<ShippingMethod>> GetActiveByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Adds a new shipping method.
    /// </summary>
    /// <param name="shippingMethod">The shipping method to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(ShippingMethod shippingMethod);

    /// <summary>
    /// Updates an existing shipping method.
    /// </summary>
    /// <param name="shippingMethod">The shipping method to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ShippingMethod shippingMethod);

    /// <summary>
    /// Deletes a shipping method by its unique identifier.
    /// </summary>
    /// <param name="id">The shipping method ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);
}
