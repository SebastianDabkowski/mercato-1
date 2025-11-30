using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for user block data access operations.
/// </summary>
public interface IUserBlockRepository
{
    /// <summary>
    /// Gets the active block for a user if one exists.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>The active block info if found; otherwise, null.</returns>
    Task<UserBlockInfo?> GetActiveBlockAsync(string userId);

    /// <summary>
    /// Gets a block record by its unique identifier.
    /// </summary>
    /// <param name="id">The block record ID.</param>
    /// <returns>The block info if found; otherwise, null.</returns>
    Task<UserBlockInfo?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets the block history for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of block records for the user, ordered by date descending.</returns>
    Task<IReadOnlyList<UserBlockInfo>> GetBlockHistoryAsync(string userId);

    /// <summary>
    /// Adds a new block record.
    /// </summary>
    /// <param name="blockInfo">The block info to add.</param>
    /// <returns>The added block info.</returns>
    Task<UserBlockInfo> AddAsync(UserBlockInfo blockInfo);

    /// <summary>
    /// Updates an existing block record.
    /// </summary>
    /// <param name="blockInfo">The block info to update.</param>
    Task UpdateAsync(UserBlockInfo blockInfo);
}
