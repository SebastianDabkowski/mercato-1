using Mercato.Notifications.Domain.Entities;

namespace Mercato.Notifications.Domain.Interfaces;

/// <summary>
/// Repository interface for notification data access operations.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Gets a notification by its unique identifier.
    /// </summary>
    /// <param name="id">The notification ID.</param>
    /// <returns>The notification if found; otherwise, null.</returns>
    Task<Notification?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets notifications for a specific user with optional filtering and pagination.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="isRead">Optional filter for read/unread notifications.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A tuple containing the list of notifications for the current page and the total count.</returns>
    Task<(IReadOnlyList<Notification> Notifications, int TotalCount)> GetByUserIdAsync(
        string userId,
        bool? isRead,
        int page,
        int pageSize);

    /// <summary>
    /// Gets the count of unread notifications for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of unread notifications.</returns>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>
    /// Adds a new notification to the repository.
    /// </summary>
    /// <param name="notification">The notification to add.</param>
    /// <returns>The added notification.</returns>
    Task<Notification> AddAsync(Notification notification);

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    /// <param name="notification">The notification to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Notification notification);

    /// <summary>
    /// Marks a specific notification as read for a user.
    /// </summary>
    /// <param name="id">The notification ID.</param>
    /// <param name="userId">The user ID (for authorization).</param>
    /// <returns>True if the notification was found and marked as read; otherwise, false.</returns>
    Task<bool> MarkAsReadAsync(Guid id, string userId);

    /// <summary>
    /// Marks all notifications as read for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The number of notifications marked as read.</returns>
    Task<int> MarkAllAsReadAsync(string userId);
}
