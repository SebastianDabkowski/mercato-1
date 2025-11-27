using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing user roles.
/// </summary>
public interface IUserRoleManagementService
{
    /// <summary>
    /// Gets all users with their assigned roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of users with their roles.</returns>
    Task<IReadOnlyList<UserWithRolesInfo>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user with their assigned roles.
    /// </summary>
    /// <param name="userId">The user ID to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user with their roles, or null if not found.</returns>
    Task<UserWithRolesInfo?> GetUserWithRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's role.
    /// </summary>
    /// <param name="command">The command containing the role change details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the role change operation.</returns>
    Task<ChangeUserRoleResult> ChangeUserRoleAsync(ChangeUserRoleCommand command, CancellationToken cancellationToken = default);
}
