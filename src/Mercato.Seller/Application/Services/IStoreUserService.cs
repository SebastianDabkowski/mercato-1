using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Services;

/// <summary>
/// Service interface for managing internal store users.
/// </summary>
public interface IStoreUserService
{
    /// <summary>
    /// Gets all internal users for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of store users.</returns>
    Task<IReadOnlyList<StoreUser>> GetStoreUsersAsync(Guid storeId);

    /// <summary>
    /// Gets a specific store user by ID.
    /// </summary>
    /// <param name="storeUserId">The store user ID.</param>
    /// <returns>The store user if found; otherwise, null.</returns>
    Task<StoreUser?> GetStoreUserByIdAsync(Guid storeUserId);

    /// <summary>
    /// Gets all stores a user has access to.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of store user records representing store access.</returns>
    Task<IReadOnlyList<StoreUser>> GetUserStoreAccessAsync(string userId);

    /// <summary>
    /// Checks if a user has access to a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user has active access; otherwise, false.</returns>
    Task<bool> HasStoreAccessAsync(Guid storeId, string userId);

    /// <summary>
    /// Gets the user's role for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The store role if the user has access; otherwise, null.</returns>
    Task<StoreRole?> GetUserRoleAsync(Guid storeId, string userId);

    /// <summary>
    /// Invites a new internal user to a store.
    /// </summary>
    /// <param name="command">The invite command.</param>
    /// <returns>The result of the invitation.</returns>
    Task<InviteStoreUserResult> InviteUserAsync(InviteStoreUserCommand command);

    /// <summary>
    /// Accepts a store user invitation.
    /// </summary>
    /// <param name="command">The accept invitation command.</param>
    /// <returns>The result of accepting the invitation.</returns>
    Task<AcceptStoreUserInvitationResult> AcceptInvitationAsync(AcceptStoreUserInvitationCommand command);

    /// <summary>
    /// Updates the role of an internal store user.
    /// </summary>
    /// <param name="command">The update role command.</param>
    /// <returns>The result of the role update.</returns>
    Task<UpdateStoreUserRoleResult> UpdateUserRoleAsync(UpdateStoreUserRoleCommand command);

    /// <summary>
    /// Deactivates an internal store user.
    /// </summary>
    /// <param name="command">The deactivate command.</param>
    /// <returns>The result of the deactivation.</returns>
    Task<DeactivateStoreUserResult> DeactivateUserAsync(DeactivateStoreUserCommand command);

    /// <summary>
    /// Validates an invitation token.
    /// </summary>
    /// <param name="token">The invitation token.</param>
    /// <returns>The store user if the token is valid and not expired; otherwise, null.</returns>
    Task<StoreUser?> ValidateInvitationTokenAsync(string token);
}
