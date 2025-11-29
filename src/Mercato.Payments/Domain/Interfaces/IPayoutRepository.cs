using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Domain.Interfaces;

/// <summary>
/// Repository interface for payout data access operations.
/// </summary>
public interface IPayoutRepository
{
    /// <summary>
    /// Gets a payout by its unique identifier.
    /// </summary>
    /// <param name="id">The payout identifier.</param>
    /// <returns>The payout if found; otherwise, null.</returns>
    Task<Payout?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all payouts for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>A read-only list of payouts for the seller.</returns>
    Task<IReadOnlyList<Payout>> GetBySellerIdAsync(Guid sellerId);

    /// <summary>
    /// Gets all payouts for a specific seller with a given status.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <param name="status">The payout status to filter by.</param>
    /// <returns>A read-only list of payouts matching the criteria.</returns>
    Task<IReadOnlyList<Payout>> GetBySellerIdAndStatusAsync(Guid sellerId, PayoutStatus status);

    /// <summary>
    /// Gets payouts for a specific seller with optional filtering by status and date range.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <param name="status">Optional payout status to filter by.</param>
    /// <param name="fromDate">Optional start date for filtering by scheduled date.</param>
    /// <param name="toDate">Optional end date for filtering by scheduled date.</param>
    /// <returns>A read-only list of payouts matching the criteria.</returns>
    Task<IReadOnlyList<Payout>> GetBySellerIdWithFiltersAsync(Guid sellerId, PayoutStatus? status, DateTimeOffset? fromDate, DateTimeOffset? toDate);

    /// <summary>
    /// Gets all payouts with a specific status.
    /// </summary>
    /// <param name="status">The payout status to filter by.</param>
    /// <returns>A read-only list of payouts with the specified status.</returns>
    Task<IReadOnlyList<Payout>> GetByStatusAsync(PayoutStatus status);

    /// <summary>
    /// Gets all payouts in a specific batch.
    /// </summary>
    /// <param name="batchId">The batch identifier.</param>
    /// <returns>A read-only list of payouts in the batch.</returns>
    Task<IReadOnlyList<Payout>> GetByBatchIdAsync(Guid batchId);

    /// <summary>
    /// Gets payouts scheduled before or on a specific date.
    /// </summary>
    /// <param name="scheduledBefore">The date to filter by.</param>
    /// <returns>A read-only list of payouts scheduled before the specified date.</returns>
    Task<IReadOnlyList<Payout>> GetScheduledPayoutsAsync(DateTimeOffset scheduledBefore);

    /// <summary>
    /// Gets payouts that are eligible for retry (failed with retry count below max).
    /// </summary>
    /// <param name="maxRetryCount">The maximum retry count.</param>
    /// <returns>A read-only list of payouts eligible for retry.</returns>
    Task<IReadOnlyList<Payout>> GetPayoutsForRetryAsync(int maxRetryCount);

    /// <summary>
    /// Adds a new payout.
    /// </summary>
    /// <param name="payout">The payout to add.</param>
    /// <returns>The added payout.</returns>
    Task<Payout> AddAsync(Payout payout);

    /// <summary>
    /// Adds multiple payouts in a single operation.
    /// </summary>
    /// <param name="payouts">The payouts to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<Payout> payouts);

    /// <summary>
    /// Updates an existing payout.
    /// </summary>
    /// <param name="payout">The payout to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Payout payout);

    /// <summary>
    /// Updates multiple payouts in a single operation.
    /// </summary>
    /// <param name="payouts">The payouts to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateRangeAsync(IEnumerable<Payout> payouts);
}
