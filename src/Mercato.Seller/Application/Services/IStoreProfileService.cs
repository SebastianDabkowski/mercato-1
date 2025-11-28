using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Services;

/// <summary>
/// Service interface for store profile management operations.
/// </summary>
public interface IStoreProfileService
{
    /// <summary>
    /// Gets the store for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <returns>The store if found; otherwise, null.</returns>
    Task<Store?> GetStoreBySellerIdAsync(string sellerId);

    /// <summary>
    /// Gets a store by its unique identifier.
    /// </summary>
    /// <param name="id">The store ID.</param>
    /// <returns>The store if found; otherwise, null.</returns>
    Task<Store?> GetStoreByIdAsync(Guid id);

    /// <summary>
    /// Gets a publicly accessible store by its SEO-friendly URL slug.
    /// Only returns stores with Active or LimitedActive status.
    /// </summary>
    /// <param name="slug">The store slug.</param>
    /// <returns>The store if found and publicly accessible; otherwise, null.</returns>
    Task<Store?> GetPublicStoreBySlugAsync(string slug);

    /// <summary>
    /// Checks if a store exists by its slug, regardless of its status.
    /// </summary>
    /// <param name="slug">The store slug.</param>
    /// <returns>True if a store with the given slug exists; otherwise, false.</returns>
    Task<bool> StoreExistsBySlugAsync(string slug);

    /// <summary>
    /// Updates an existing store's profile.
    /// </summary>
    /// <param name="command">The update store profile command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateStoreProfileResult> UpdateStoreProfileAsync(UpdateStoreProfileCommand command);

    /// <summary>
    /// Creates a new store if one doesn't exist, otherwise updates the existing store.
    /// </summary>
    /// <param name="command">The update store profile command.</param>
    /// <returns>The result of the create or update operation.</returns>
    Task<UpdateStoreProfileResult> CreateOrUpdateStoreProfileAsync(UpdateStoreProfileCommand command);

    /// <summary>
    /// Gets stores by their unique identifiers.
    /// </summary>
    /// <param name="ids">The store IDs to retrieve.</param>
    /// <returns>A list of stores matching the provided IDs.</returns>
    Task<IReadOnlyList<Store>> GetStoresByIdsAsync(IEnumerable<Guid> ids);
}
