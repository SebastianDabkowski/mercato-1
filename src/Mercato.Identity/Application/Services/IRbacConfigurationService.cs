using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Queries;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for managing RBAC (Role-Based Access Control) configuration.
/// </summary>
public interface IRbacConfigurationService
{
    /// <summary>
    /// Gets all available permissions in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all permissions.</returns>
    Task<IReadOnlyList<PermissionInfo>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available roles in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all role names.</returns>
    Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the RBAC configuration for all roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of role-permission configurations.</returns>
    Task<IReadOnlyList<RolePermissionConfiguration>> GetRbacConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the RBAC configuration for a specific role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role-permission configuration, or null if the role doesn't exist.</returns>
    Task<RolePermissionConfiguration?> GetRoleConfigurationAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by module for a specific role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="module">The module name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of permissions for the specified module and role.</returns>
    Task<IReadOnlyList<PermissionInfo>> GetPermissionsByModuleAsync(string roleName, string module, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a permission to a role.
    /// </summary>
    /// <param name="command">The command containing assignment details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the assignment operation.</returns>
    Task<AssignPermissionResult> AssignPermissionAsync(AssignPermissionCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a permission from a role.
    /// </summary>
    /// <param name="command">The command containing revocation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the revocation operation.</returns>
    Task<RevokePermissionResult> RevokePermissionAsync(RevokePermissionCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role has a specific permission.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role has the permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(string roleName, string permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct module names in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of module names.</returns>
    Task<IReadOnlyList<string>> GetModulesAsync(CancellationToken cancellationToken = default);
}
