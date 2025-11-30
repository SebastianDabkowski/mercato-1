using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for data processing activity history management.
/// </summary>
public interface IDataProcessingActivityHistoryRepository
{
    /// <summary>
    /// Gets all history records for a specific data processing activity.
    /// </summary>
    /// <param name="activityId">The data processing activity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of history records for the activity.</returns>
    Task<IReadOnlyList<DataProcessingActivityHistory>> GetByActivityIdAsync(Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new history record.
    /// </summary>
    /// <param name="history">The history record to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added history record.</returns>
    Task<DataProcessingActivityHistory> AddAsync(DataProcessingActivityHistory history, CancellationToken cancellationToken = default);
}
