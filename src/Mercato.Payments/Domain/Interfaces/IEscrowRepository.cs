using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Domain.Interfaces;

/// <summary>
/// Repository interface for escrow entry data access operations.
/// </summary>
public interface IEscrowRepository
{
    /// <summary>
    /// Gets an escrow entry by its unique identifier.
    /// </summary>
    /// <param name="id">The escrow entry identifier.</param>
    /// <returns>The escrow entry if found; otherwise, null.</returns>
    Task<EscrowEntry?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all escrow entries for a specific order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>A read-only list of escrow entries for the order.</returns>
    Task<IReadOnlyList<EscrowEntry>> GetByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Gets all escrow entries for a specific payment transaction.
    /// </summary>
    /// <param name="paymentTransactionId">The payment transaction identifier.</param>
    /// <returns>A read-only list of escrow entries for the payment transaction.</returns>
    Task<IReadOnlyList<EscrowEntry>> GetByPaymentTransactionIdAsync(Guid paymentTransactionId);

    /// <summary>
    /// Gets all escrow entries for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>A read-only list of escrow entries for the seller.</returns>
    Task<IReadOnlyList<EscrowEntry>> GetBySellerIdAsync(Guid sellerId);

    /// <summary>
    /// Gets all escrow entries for a specific seller with a given status.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <param name="status">The escrow status to filter by.</param>
    /// <returns>A read-only list of escrow entries matching the criteria.</returns>
    Task<IReadOnlyList<EscrowEntry>> GetBySellerIdAndStatusAsync(Guid sellerId, EscrowStatus status);

    /// <summary>
    /// Adds a new escrow entry.
    /// </summary>
    /// <param name="entry">The escrow entry to add.</param>
    /// <returns>The added escrow entry.</returns>
    Task<EscrowEntry> AddAsync(EscrowEntry entry);

    /// <summary>
    /// Adds multiple escrow entries in a single operation.
    /// </summary>
    /// <param name="entries">The escrow entries to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<EscrowEntry> entries);

    /// <summary>
    /// Updates an existing escrow entry.
    /// </summary>
    /// <param name="entry">The escrow entry to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(EscrowEntry entry);

    /// <summary>
    /// Updates multiple escrow entries in a single operation.
    /// </summary>
    /// <param name="entries">The escrow entries to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateRangeAsync(IEnumerable<EscrowEntry> entries);
}
