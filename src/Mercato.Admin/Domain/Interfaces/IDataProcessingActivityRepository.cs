using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for data processing activity management.
/// </summary>
public interface IDataProcessingActivityRepository
{
    /// <summary>
    /// Gets a data processing activity by its unique identifier.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The data processing activity if found; otherwise, null.</returns>
    Task<DataProcessingActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all data processing activities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all data processing activities.</returns>
    Task<IReadOnlyList<DataProcessingActivity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active data processing activities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active data processing activities.</returns>
    Task<IReadOnlyList<DataProcessingActivity>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new data processing activity.
    /// </summary>
    /// <param name="activity">The activity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added data processing activity.</returns>
    Task<DataProcessingActivity> AddAsync(DataProcessingActivity activity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing data processing activity.
    /// </summary>
    /// <param name="activity">The activity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(DataProcessingActivity activity, CancellationToken cancellationToken = default);
}
