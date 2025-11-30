using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for VAT rule management.
/// </summary>
public interface IVatRuleRepository
{
    /// <summary>
    /// Gets a VAT rule by its unique identifier.
    /// </summary>
    /// <param name="id">The VAT rule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The VAT rule if found; otherwise, null.</returns>
    Task<VatRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all VAT rules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all VAT rules.</returns>
    Task<IReadOnlyList<VatRule>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the applicable active VAT rule for a specific country, optional category, and date.
    /// Returns the highest priority rule that matches the criteria and is effective for the given date.
    /// </summary>
    /// <param name="countryCode">The ISO 3166-1 alpha-2 country code.</param>
    /// <param name="categoryId">The optional category ID.</param>
    /// <param name="asOfDate">The date for which to find the applicable rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The applicable VAT rule if found; otherwise, null.</returns>
    Task<VatRule?> GetActiveByCountryAsync(
        string countryCode,
        Guid? categoryId,
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new VAT rule.
    /// </summary>
    /// <param name="vatRule">The VAT rule to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added VAT rule.</returns>
    Task<VatRule> AddAsync(VatRule vatRule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing VAT rule.
    /// </summary>
    /// <param name="vatRule">The VAT rule to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(VatRule vatRule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a VAT rule by its ID.
    /// </summary>
    /// <param name="id">The VAT rule ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
