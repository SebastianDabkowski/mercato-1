using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Services;

/// <summary>
/// Service interface for shipping method management operations.
/// </summary>
public interface IShippingMethodService
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
    /// Creates a new shipping method for a store.
    /// </summary>
    /// <param name="command">The create shipping method command.</param>
    /// <returns>The result of the create operation.</returns>
    Task<CreateShippingMethodResult> CreateAsync(CreateShippingMethodCommand command);

    /// <summary>
    /// Updates an existing shipping method.
    /// </summary>
    /// <param name="command">The update shipping method command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateShippingMethodResult> UpdateAsync(UpdateShippingMethodCommand command);

    /// <summary>
    /// Deletes a shipping method.
    /// </summary>
    /// <param name="command">The delete shipping method command.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<DeleteShippingMethodResult> DeleteAsync(DeleteShippingMethodCommand command);
}
