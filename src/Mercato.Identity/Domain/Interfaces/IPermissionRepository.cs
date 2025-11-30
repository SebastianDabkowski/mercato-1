using Mercato.Identity.Domain.Entities;

namespace Mercato.Identity.Domain.Interfaces;

/// <summary>
/// Repository interface for managing permissions.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// Gets all available permissions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all permissions.</returns>
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a permission by its ID.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The permission, or null if not found.</returns>
    Task<Permission?> GetByIdAsync(string permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions by module name.
    /// </summary>
    /// <param name="module">The module name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of permissions for the specified module.</returns>
    Task<IReadOnlyList<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new permission.
    /// </summary>
    /// <param name="permission">The permission to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing permission.
    /// </summary>
    /// <param name="permission">The permission to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a permission by its ID.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string permissionId, CancellationToken cancellationToken = default);
}
