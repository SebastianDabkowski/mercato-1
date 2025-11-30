using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for currency history management.
/// </summary>
public interface ICurrencyHistoryRepository
{
    /// <summary>
    /// Adds a new history record.
    /// </summary>
    /// <param name="history">The history record to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added history record.</returns>
    Task<CurrencyHistory> AddAsync(CurrencyHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all history records for a specific currency, ordered by change date descending.
    /// </summary>
    /// <param name="currencyId">The currency ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of history records for the currency.</returns>
    Task<IReadOnlyList<CurrencyHistory>> GetByCurrencyIdAsync(Guid currencyId, CancellationToken cancellationToken = default);
}
