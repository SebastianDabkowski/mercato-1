using Mercato.Payments.Domain.Entities;

namespace Mercato.Payments.Domain.Interfaces;

/// <summary>
/// Repository interface for refund data access operations.
/// </summary>
public interface IRefundRepository
{
    /// <summary>
    /// Gets a refund by its unique identifier.
    /// </summary>
    /// <param name="id">The refund identifier.</param>
    /// <returns>The refund if found; otherwise, null.</returns>
    Task<Refund?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all refunds for a specific payment transaction.
    /// </summary>
    /// <param name="paymentTransactionId">The payment transaction identifier.</param>
    /// <returns>A read-only list of refunds for the payment transaction.</returns>
    Task<IReadOnlyList<Refund>> GetByPaymentTransactionIdAsync(Guid paymentTransactionId);

    /// <summary>
    /// Gets all refunds for a specific order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>A read-only list of refunds for the order.</returns>
    Task<IReadOnlyList<Refund>> GetByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Gets all refunds for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>A read-only list of refunds for the seller.</returns>
    Task<IReadOnlyList<Refund>> GetBySellerIdAsync(Guid sellerId);

    /// <summary>
    /// Gets the total refunded amount for an order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>The total refunded amount.</returns>
    Task<decimal> GetTotalRefundedByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Gets the total refunded amount for a specific order and seller.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="sellerId">The seller identifier.</param>
    /// <returns>The total refunded amount for the order and seller.</returns>
    Task<decimal> GetTotalRefundedByOrderIdAndSellerIdAsync(Guid orderId, Guid sellerId);

    /// <summary>
    /// Adds a new refund.
    /// </summary>
    /// <param name="refund">The refund to add.</param>
    /// <returns>The added refund.</returns>
    Task<Refund> AddAsync(Refund refund);

    /// <summary>
    /// Updates an existing refund.
    /// </summary>
    /// <param name="refund">The refund to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Refund refund);
}
