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
    /// Gets filtered and paginated return requests for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="statuses">Optional list of statuses to filter by.</param>
    /// <param name="fromDate">Optional start date for date range filter.</param>
    /// <param name="toDate">Optional end date for date range filter.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A tuple containing the filtered return requests and total count.</returns>
    Task<(IReadOnlyList<ReturnRequest> ReturnRequests, int TotalCount)> GetFilteredByStoreIdAsync(
        Guid storeId,
        IReadOnlyList<ReturnStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize);

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

    /// <summary>
    /// Gets all return requests with filtering and pagination for admin view.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter by case number, buyer ID, or store name.</param>
    /// <param name="statuses">Optional list of statuses to filter by.</param>
    /// <param name="caseTypes">Optional list of case types to filter by.</param>
    /// <param name="fromDate">Optional start date for date range filter.</param>
    /// <param name="toDate">Optional end date for date range filter.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A tuple containing the filtered return requests and total count.</returns>
    Task<(IReadOnlyList<ReturnRequest> ReturnRequests, int TotalCount)> GetAllFilteredAsync(
        string? searchTerm,
        IReadOnlyList<ReturnStatus>? statuses,
        IReadOnlyList<CaseType>? caseTypes,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize);

    /// <summary>
    /// Gets a return request by ID with status history loaded.
    /// </summary>
    /// <param name="id">The return request ID.</param>
    /// <returns>The return request with history if found; otherwise, null.</returns>
    Task<ReturnRequest?> GetByIdWithHistoryAsync(Guid id);
}
