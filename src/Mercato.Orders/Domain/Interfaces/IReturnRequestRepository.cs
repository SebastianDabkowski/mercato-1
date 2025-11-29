using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for return request data access operations.
/// </summary>
public interface IReturnRequestRepository
{
    /// <summary>
    /// Gets a return request by its unique identifier.
    /// </summary>
    /// <param name="id">The return request ID.</param>
    /// <returns>The return request if found; otherwise, null.</returns>
    Task<ReturnRequest?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets the return request for a specific seller sub-order.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order ID.</param>
    /// <returns>The return request if found; otherwise, null.</returns>
    Task<ReturnRequest?> GetBySellerSubOrderIdAsync(Guid sellerSubOrderId);

    /// <summary>
    /// Gets all return requests for a specific buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A list of return requests for the buyer.</returns>
    Task<IReadOnlyList<ReturnRequest>> GetByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Gets all return requests for sub-orders belonging to a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of return requests for the store's sub-orders.</returns>
    Task<IReadOnlyList<ReturnRequest>> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets open (non-completed/rejected) return requests that include specific item IDs.
    /// Used to prevent duplicate cases for the same line items.
    /// </summary>
    /// <param name="itemIds">The seller sub-order item IDs to check.</param>
    /// <returns>A list of open return requests that include any of the specified items.</returns>
    Task<IReadOnlyList<ReturnRequest>> GetOpenCasesForItemsAsync(IEnumerable<Guid> itemIds);

    /// <summary>
    /// Adds a new return request to the repository.
    /// </summary>
    /// <param name="returnRequest">The return request to add.</param>
    /// <returns>The added return request.</returns>
    Task<ReturnRequest> AddAsync(ReturnRequest returnRequest);

    /// <summary>
    /// Updates an existing return request.
    /// </summary>
    /// <param name="returnRequest">The return request to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ReturnRequest returnRequest);
}
