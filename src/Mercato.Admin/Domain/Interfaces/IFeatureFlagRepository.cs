using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for feature flag management.
/// </summary>
public interface IFeatureFlagRepository
{
    /// <summary>
    /// Gets a feature flag by its unique identifier.
    /// </summary>
    /// <param name="id">The feature flag ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The feature flag if found; otherwise, null.</returns>
    Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a feature flag by its key and environment.
    /// </summary>
    /// <param name="key">The feature flag key.</param>
    /// <param name="environment">The target environment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The feature flag if found; otherwise, null.</returns>
    Task<FeatureFlag?> GetByKeyAndEnvironmentAsync(string key, FeatureFlagEnvironment environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all feature flags.</returns>
    Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature flags for a specific environment.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of feature flags for the specified environment.</returns>
    Task<IReadOnlyList<FeatureFlag>> GetByEnvironmentAsync(FeatureFlagEnvironment environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new feature flag.
    /// </summary>
    /// <param name="featureFlag">The feature flag to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added feature flag.</returns>
    Task<FeatureFlag> AddAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing feature flag.
    /// </summary>
    /// <param name="featureFlag">The feature flag to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a feature flag by its ID.
    /// </summary>
    /// <param name="id">The feature flag ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
