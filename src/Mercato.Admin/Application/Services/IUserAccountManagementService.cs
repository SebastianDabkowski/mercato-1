using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for managing and querying user accounts.
/// </summary>
public interface IUserAccountManagementService
{
    /// <summary>
    /// Gets a paginated and filtered list of user accounts.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of user accounts.</returns>
    Task<PagedResult<UserAccountInfo>> GetUsersAsync(
        UserAccountFilterQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific user.
    /// </summary>
    /// <param name="userId">The user ID to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detailed user information, or null if not found.</returns>
    Task<UserDetailInfo?> GetUserDetailAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
