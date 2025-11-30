using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Entities;

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

    /// <summary>
    /// Blocks a user account.
    /// </summary>
    /// <param name="command">The block user command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the block operation.</returns>
    Task<BlockUserResult> BlockUserAsync(
        BlockUserCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblocks a user account.
    /// </summary>
    /// <param name="command">The unblock user command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the unblock operation.</returns>
    Task<UnblockUserResult> UnblockUserAsync(
        UnblockUserCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active block for a user if one exists.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active block info if found; otherwise, null.</returns>
    Task<UserBlockInfo?> GetActiveBlockAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the block/reactivate history for a user.
    /// </summary>
    /// <param name="userId">The user ID to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of block history records, ordered by date descending.</returns>
    Task<IReadOnlyList<BlockHistoryInfo>> GetBlockHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
