using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for SLA configuration management.
/// </summary>
public interface ISlaConfigurationRepository
{
    /// <summary>
    /// Gets an SLA configuration by its unique identifier.
    /// </summary>
    /// <param name="id">The configuration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SLA configuration if found; otherwise, null.</returns>
    Task<SlaConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active SLA configurations ordered by priority.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active SLA configurations.</returns>
    Task<IReadOnlyList<SlaConfiguration>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the applicable SLA configuration for a case type and category.
    /// Returns the highest priority (lowest number) matching configuration.
    /// </summary>
    /// <param name="caseType">The case type (e.g., "Return", "Complaint").</param>
    /// <param name="category">The category (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The applicable SLA configuration, or the default if none matches.</returns>
    Task<SlaConfiguration?> GetApplicableConfigurationAsync(
        string? caseType,
        string? category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new SLA configuration.
    /// </summary>
    /// <param name="configuration">The configuration to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added configuration.</returns>
    Task<SlaConfiguration> AddAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing SLA configuration.
    /// </summary>
    /// <param name="configuration">The configuration to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(SlaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an SLA configuration.
    /// </summary>
    /// <param name="id">The configuration ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
