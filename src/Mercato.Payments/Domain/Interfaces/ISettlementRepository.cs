using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Domain.Interfaces;

/// <summary>
/// Repository interface for settlement data access operations.
/// </summary>
public interface ISettlementRepository
{
    /// <summary>
    /// Gets a settlement by its unique identifier.
    /// </summary>
    /// <param name="id">The settlement identifier.</param>
    /// <returns>The settlement if found; otherwise, null.</returns>
    Task<Settlement?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a settlement by its unique identifier including line items.
    /// </summary>
    /// <param name="id">The settlement identifier.</param>
    /// <returns>The settlement with line items if found; otherwise, null.</returns>
    Task<Settlement?> GetByIdWithLineItemsAsync(Guid id);

    /// <summary>
    /// Gets a settlement for a specific seller and period.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <param name="year">The settlement year.</param>
    /// <param name="month">The settlement month.</param>
    /// <returns>The settlement if found; otherwise, null.</returns>
    Task<Settlement?> GetBySellerAndPeriodAsync(Guid sellerId, int year, int month);

    /// <summary>
    /// Gets all settlements for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>A read-only list of settlements for the seller.</returns>
    Task<IReadOnlyList<Settlement>> GetBySellerIdAsync(Guid sellerId);

    /// <summary>
    /// Gets all settlements for a specific period.
    /// </summary>
    /// <param name="year">The settlement year.</param>
    /// <param name="month">The settlement month.</param>
    /// <returns>A read-only list of settlements for the period.</returns>
    Task<IReadOnlyList<Settlement>> GetByPeriodAsync(int year, int month);

    /// <summary>
    /// Gets settlements with optional filters.
    /// </summary>
    /// <param name="sellerId">Optional seller identifier filter.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="month">Optional month filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>A read-only list of settlements matching the filters.</returns>
    Task<IReadOnlyList<Settlement>> GetFilteredAsync(
        Guid? sellerId,
        int? year,
        int? month,
        SettlementStatus? status);

    /// <summary>
    /// Adds a new settlement.
    /// </summary>
    /// <param name="settlement">The settlement to add.</param>
    /// <returns>The added settlement.</returns>
    Task<Settlement> AddAsync(Settlement settlement);

    /// <summary>
    /// Updates an existing settlement.
    /// </summary>
    /// <param name="settlement">The settlement to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Settlement settlement);

    /// <summary>
    /// Adds a line item to a settlement.
    /// </summary>
    /// <param name="lineItem">The line item to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddLineItemAsync(SettlementLineItem lineItem);

    /// <summary>
    /// Deletes all line items for a settlement (for regeneration).
    /// </summary>
    /// <param name="settlementId">The settlement identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteLineItemsAsync(Guid settlementId);
}
