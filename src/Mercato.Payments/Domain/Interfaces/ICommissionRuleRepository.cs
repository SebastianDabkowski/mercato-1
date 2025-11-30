using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Domain.Interfaces;

/// <summary>
/// Repository interface for commission rule data access operations.
/// </summary>
public interface ICommissionRuleRepository
{
    /// <summary>
    /// Gets a commission rule by its unique identifier.
    /// </summary>
    /// <param name="id">The commission rule identifier.</param>
    /// <returns>The commission rule if found; otherwise, null.</returns>
    Task<CommissionRule?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all commission rules (both active and inactive) ordered by priority (descending).
    /// </summary>
    /// <returns>A read-only list of all commission rules.</returns>
    Task<IReadOnlyList<CommissionRule>> GetAllRulesAsync();

    /// <summary>
    /// Gets all active commission rules ordered by priority (descending).
    /// </summary>
    /// <returns>A read-only list of active commission rules.</returns>
    Task<IReadOnlyList<CommissionRule>> GetActiveRulesAsync();

    /// <summary>
    /// Finds the best matching active rule for a given seller and/or category at a specific point in time.
    /// Rules are matched by priority with the following precedence:
    /// 1. Seller + Category match (highest priority)
    /// 2. Seller-only match
    /// 3. Category-only match
    /// 4. Global default (SellerId and CategoryId both null)
    /// Only rules with EffectiveDate on or before the asOfDate are considered.
    /// </summary>
    /// <param name="sellerId">The seller identifier (optional).</param>
    /// <param name="categoryId">The category identifier (optional).</param>
    /// <param name="asOfDate">The date to evaluate rule effectiveness against. If null, uses current date.</param>
    /// <returns>The best matching commission rule if found; otherwise, null.</returns>
    Task<CommissionRule?> GetBestMatchingRuleAsync(Guid? sellerId, string? categoryId, DateTimeOffset? asOfDate = null);

    /// <summary>
    /// Finds rules that overlap with the specified scope and effective date range.
    /// Used for conflict detection when creating or editing rules.
    /// </summary>
    /// <param name="sellerId">The seller identifier (optional).</param>
    /// <param name="categoryId">The category identifier (optional).</param>
    /// <param name="effectiveDate">The effective date of the new/updated rule.</param>
    /// <param name="excludeRuleId">Rule ID to exclude from conflict check (for updates).</param>
    /// <returns>A read-only list of conflicting rules.</returns>
    Task<IReadOnlyList<CommissionRule>> GetConflictingRulesAsync(
        Guid? sellerId,
        string? categoryId,
        DateTimeOffset effectiveDate,
        Guid? excludeRuleId = null);

    /// <summary>
    /// Adds a new commission rule.
    /// </summary>
    /// <param name="rule">The commission rule to add.</param>
    /// <returns>The added commission rule.</returns>
    Task<CommissionRule> AddAsync(CommissionRule rule);

    /// <summary>
    /// Updates an existing commission rule.
    /// </summary>
    /// <param name="rule">The commission rule to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(CommissionRule rule);
}
