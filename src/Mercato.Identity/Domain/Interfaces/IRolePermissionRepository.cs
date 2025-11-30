using Mercato.Identity.Domain.Entities;

namespace Mercato.Identity.Domain.Interfaces;

/// <summary>
/// Repository interface for managing role-permission mappings.
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// Gets all role-permission mappings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all role-permission mappings.</returns>
    Task<IReadOnlyList<RolePermission>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions assigned to a specific role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of role-permission mappings for the specified role.</returns>
    Task<IReadOnlyList<RolePermission>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role has a specific permission.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role has the permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(string roleName, string permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a permission to a role.
    /// </summary>
    /// <param name="rolePermission">The role-permission mapping to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a permission from a role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(string roleName, string permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all permissions from a role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAllForRoleAsync(string roleName, CancellationToken cancellationToken = default);
}
