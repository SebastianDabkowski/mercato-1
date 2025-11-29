using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Domain.Interfaces;

/// <summary>
/// Repository interface for commission record data access operations.
/// </summary>
public interface ICommissionRecordRepository
{
    /// <summary>
    /// Gets a commission record by its unique identifier.
    /// </summary>
    /// <param name="id">The commission record identifier.</param>
    /// <returns>The commission record if found; otherwise, null.</returns>
    Task<CommissionRecord?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all commission records for a specific payment transaction.
    /// </summary>
    /// <param name="paymentTransactionId">The payment transaction identifier.</param>
    /// <returns>A read-only list of commission records for the payment transaction.</returns>
    Task<IReadOnlyList<CommissionRecord>> GetByPaymentTransactionIdAsync(Guid paymentTransactionId);

    /// <summary>
    /// Gets all commission records for a specific order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>A read-only list of commission records for the order.</returns>
    Task<IReadOnlyList<CommissionRecord>> GetByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Gets all commission records for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>A read-only list of commission records for the seller.</returns>
    Task<IReadOnlyList<CommissionRecord>> GetBySellerIdAsync(Guid sellerId);

    /// <summary>
    /// Gets a commission record for a specific order and seller combination.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>The commission record if found; otherwise, null.</returns>
    Task<CommissionRecord?> GetByOrderIdAndSellerIdAsync(Guid orderId, Guid sellerId);

    /// <summary>
    /// Adds a new commission record.
    /// </summary>
    /// <param name="record">The commission record to add.</param>
    /// <returns>The added commission record.</returns>
    Task<CommissionRecord> AddAsync(CommissionRecord record);

    /// <summary>
    /// Adds multiple commission records in a single operation.
    /// </summary>
    /// <param name="records">The commission records to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<CommissionRecord> records);

    /// <summary>
    /// Updates an existing commission record.
    /// </summary>
    /// <param name="record">The commission record to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(CommissionRecord record);

    /// <summary>
    /// Updates multiple commission records in a single operation.
    /// </summary>
    /// <param name="records">The commission records to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateRangeAsync(IEnumerable<CommissionRecord> records);
}
