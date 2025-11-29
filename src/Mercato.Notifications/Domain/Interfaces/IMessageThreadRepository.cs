using Mercato.Notifications.Domain.Entities;

namespace Mercato.Notifications.Domain.Interfaces;

/// <summary>
/// Repository interface for message thread data access operations.
/// </summary>
public interface IMessageThreadRepository
{
    /// <summary>
    /// Gets a message thread by its unique identifier.
    /// </summary>
    /// <param name="id">The thread ID.</param>
    /// <returns>The thread if found; otherwise, null.</returns>
    Task<MessageThread?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets message threads for a specific product with pagination (for public Q&A).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of threads for the current page and the total count.</returns>
    Task<(IReadOnlyList<MessageThread> Threads, int TotalCount)> GetByProductIdAsync(
        Guid productId,
        int page,
        int pageSize);

    /// <summary>
    /// Gets message threads for a specific buyer with pagination.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of threads for the current page and the total count.</returns>
    Task<(IReadOnlyList<MessageThread> Threads, int TotalCount)> GetByBuyerIdAsync(
        string buyerId,
        int page,
        int pageSize);

    /// <summary>
    /// Gets message threads for a specific seller with pagination.
    /// </summary>
    /// <param name="sellerId">The seller ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of threads for the current page and the total count.</returns>
    Task<(IReadOnlyList<MessageThread> Threads, int TotalCount)> GetBySellerIdAsync(
        string sellerId,
        int page,
        int pageSize);

    /// <summary>
    /// Gets all message threads with pagination (for admin moderation).
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of threads for the current page and the total count.</returns>
    Task<(IReadOnlyList<MessageThread> Threads, int TotalCount)> GetAllAsync(
        int page,
        int pageSize);

    /// <summary>
    /// Adds a new message thread to the repository.
    /// </summary>
    /// <param name="thread">The thread to add.</param>
    /// <returns>The added thread.</returns>
    Task<MessageThread> AddAsync(MessageThread thread);

    /// <summary>
    /// Updates an existing message thread.
    /// </summary>
    /// <param name="thread">The thread to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(MessageThread thread);

    /// <summary>
    /// Checks if a user can access a specific thread.
    /// </summary>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <returns>True if the user can access the thread; otherwise, false.</returns>
    Task<bool> CanAccessAsync(Guid threadId, string userId, bool isAdmin);
}
