using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for currency management.
/// </summary>
public interface ICurrencyRepository
{
    /// <summary>
    /// Gets a currency by its unique identifier.
    /// </summary>
    /// <param name="id">The currency ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The currency if found; otherwise, null.</returns>
    Task<Currency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a currency by its ISO 4217 code.
    /// </summary>
    /// <param name="code">The currency code (e.g., USD, EUR).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The currency if found; otherwise, null.</returns>
    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all currencies.</returns>
    Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled currencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all enabled currencies.</returns>
    Task<IReadOnlyList<Currency>> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current base currency.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The base currency if found; otherwise, null.</returns>
    Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new currency.
    /// </summary>
    /// <param name="currency">The currency to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added currency.</returns>
    Task<Currency> AddAsync(Currency currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing currency.
    /// </summary>
    /// <param name="currency">The currency to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Currency currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a currency by its ID.
    /// </summary>
    /// <param name="id">The currency ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
