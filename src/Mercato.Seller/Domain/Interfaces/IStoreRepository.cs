using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for store operations.
/// </summary>
public interface IStoreRepository
{
    /// <summary>
    /// Gets the store for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The store if found; otherwise, null.</returns>
    Task<Store?> GetBySellerIdAsync(string sellerId);

    /// <summary>
    /// Gets a store by its unique identifier.
    /// </summary>
    /// <param name="id">The store ID.</param>
    /// <returns>The store if found; otherwise, null.</returns>
    Task<Store?> GetByIdAsync(Guid id);

    /// <summary>
    /// Checks if a store name is unique across all stores.
    /// </summary>
    /// <param name="name">The store name to check.</param>
    /// <param name="excludeSellerId">Optional seller ID to exclude from the check (for updates).</param>
    /// <returns>True if the name is unique; otherwise, false.</returns>
    Task<bool> IsStoreNameUniqueAsync(string name, string? excludeSellerId = null);

    /// <summary>
    /// Creates a new store.
    /// </summary>
    /// <param name="store">The store to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(Store store);

    /// <summary>
    /// Updates an existing store.
    /// </summary>
    /// <param name="store">The store to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Store store);
}
