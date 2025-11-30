using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for VAT rule history management.
/// </summary>
public interface IVatRuleHistoryRepository
{
    /// <summary>
    /// Gets all history records for a specific VAT rule, ordered by change date descending.
    /// </summary>
    /// <param name="vatRuleId">The VAT rule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of history records for the VAT rule.</returns>
    Task<IReadOnlyList<VatRuleHistory>> GetByVatRuleIdAsync(Guid vatRuleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new history record.
    /// </summary>
    /// <param name="history">The history record to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added history record.</returns>
    Task<VatRuleHistory> AddAsync(VatRuleHistory history, CancellationToken cancellationToken = default);
}
