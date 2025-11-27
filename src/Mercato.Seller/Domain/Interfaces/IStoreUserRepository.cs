using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for managing store users (internal team members).
/// </summary>
public interface IStoreUserRepository
{
    /// <summary>
    /// Gets a store user by its unique identifier.
    /// </summary>
    /// <param name="id">The store user ID.</param>
    /// <returns>The store user if found; otherwise, null.</returns>
    Task<StoreUser?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a store user by their invitation token.
    /// </summary>
    /// <param name="token">The invitation token.</param>
    /// <returns>The store user if found; otherwise, null.</returns>
    Task<StoreUser?> GetByInvitationTokenAsync(string token);

    /// <summary>
    /// Gets a store user by store ID and user ID.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The store user if found; otherwise, null.</returns>
    Task<StoreUser?> GetByStoreAndUserIdAsync(Guid storeId, string userId);

    /// <summary>
    /// Gets a store user by store ID and email.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="email">The email address.</param>
    /// <returns>The store user if found; otherwise, null.</returns>
    Task<StoreUser?> GetByStoreAndEmailAsync(Guid storeId, string email);

    /// <summary>
    /// Gets all store users for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of store users.</returns>
    Task<IReadOnlyList<StoreUser>> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets all stores a user has access to.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of store users representing store access.</returns>
    Task<IReadOnlyList<StoreUser>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Creates a new store user record.
    /// </summary>
    /// <param name="storeUser">The store user to create.</param>
    Task CreateAsync(StoreUser storeUser);

    /// <summary>
    /// Updates an existing store user record.
    /// </summary>
    /// <param name="storeUser">The store user to update.</param>
    Task UpdateAsync(StoreUser storeUser);

    /// <summary>
    /// Checks if a user with the given email already exists for the store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="email">The email address.</param>
    /// <returns>True if the email already exists for the store; otherwise, false.</returns>
    Task<bool> EmailExistsForStoreAsync(Guid storeId, string email);
}
