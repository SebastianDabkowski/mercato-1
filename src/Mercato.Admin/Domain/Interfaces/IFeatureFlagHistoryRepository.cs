using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for feature flag history management.
/// </summary>
public interface IFeatureFlagHistoryRepository
{
    /// <summary>
    /// Gets all history records for a specific feature flag, ordered by change date descending.
    /// </summary>
    /// <param name="featureFlagId">The feature flag ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of history records for the feature flag.</returns>
    Task<IReadOnlyList<FeatureFlagHistory>> GetByFeatureFlagIdAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new history record.
    /// </summary>
    /// <param name="history">The history record to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added history record.</returns>
    Task<FeatureFlagHistory> AddAsync(FeatureFlagHistory history, CancellationToken cancellationToken = default);
}
