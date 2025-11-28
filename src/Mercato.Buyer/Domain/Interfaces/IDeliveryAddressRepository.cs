using Mercato.Buyer.Domain.Entities;

namespace Mercato.Buyer.Domain.Interfaces;

/// <summary>
/// Repository interface for delivery address data access operations.
/// </summary>
public interface IDeliveryAddressRepository
{
    /// <summary>
    /// Gets a delivery address by its unique identifier.
    /// </summary>
    /// <param name="id">The delivery address ID.</param>
    /// <returns>The delivery address if found; otherwise, null.</returns>
    Task<DeliveryAddress?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all delivery addresses for a buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A read-only list of delivery addresses.</returns>
    Task<IReadOnlyList<DeliveryAddress>> GetByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Gets the default delivery address for a buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The default delivery address if found; otherwise, null.</returns>
    Task<DeliveryAddress?> GetDefaultByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Adds a new delivery address to the repository.
    /// </summary>
    /// <param name="address">The delivery address to add.</param>
    /// <returns>The added delivery address.</returns>
    Task<DeliveryAddress> AddAsync(DeliveryAddress address);

    /// <summary>
    /// Updates an existing delivery address.
    /// </summary>
    /// <param name="address">The delivery address to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(DeliveryAddress address);

    /// <summary>
    /// Deletes a delivery address.
    /// </summary>
    /// <param name="address">The delivery address to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(DeliveryAddress address);
}
