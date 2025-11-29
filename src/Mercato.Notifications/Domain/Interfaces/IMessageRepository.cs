using Mercato.Notifications.Domain.Entities;

namespace Mercato.Notifications.Domain.Interfaces;

/// <summary>
/// Repository interface for message data access operations.
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Gets a message by its unique identifier.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <returns>The message if found; otherwise, null.</returns>
    Task<Message?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets messages for a specific thread with pagination.
    /// </summary>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of messages for the current page and the total count.</returns>
    Task<(IReadOnlyList<Message> Messages, int TotalCount)> GetByThreadIdAsync(
        Guid threadId,
        int page,
        int pageSize);

    /// <summary>
    /// Adds a new message to the repository.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <returns>The added message.</returns>
    Task<Message> AddAsync(Message message);

    /// <summary>
    /// Marks a specific message as read for a user.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="userId">The user ID (for authorization).</param>
    /// <returns>True if the message was found and marked as read; otherwise, false.</returns>
    Task<bool> MarkAsReadAsync(Guid id, string userId);
}
