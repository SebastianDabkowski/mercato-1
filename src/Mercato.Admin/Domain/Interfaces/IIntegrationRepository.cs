using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for integration management.
/// </summary>
public interface IIntegrationRepository
{
    /// <summary>
    /// Gets an integration by its unique identifier.
    /// </summary>
    /// <param name="id">The integration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration if found; otherwise, null.</returns>
    Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all integrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all integrations.</returns>
    Task<IReadOnlyList<Integration>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all integrations of a specific type.
    /// </summary>
    /// <param name="type">The integration type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of integrations of the specified type.</returns>
    Task<IReadOnlyList<Integration>> GetByTypeAsync(IntegrationType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled integrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all enabled integrations.</returns>
    Task<IReadOnlyList<Integration>> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new integration.
    /// </summary>
    /// <param name="integration">The integration to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added integration.</returns>
    Task<Integration> AddAsync(Integration integration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing integration.
    /// </summary>
    /// <param name="integration">The integration to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Integration integration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an integration by its ID.
    /// </summary>
    /// <param name="id">The integration ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
