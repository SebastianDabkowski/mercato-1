using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Domain.Entities;

namespace Mercato.Notifications.Application.Services;

/// <summary>
/// Service interface for notification operations.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets notifications for a specific user with optional filtering and pagination.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="isRead">Optional filter for read/unread notifications.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>A result containing the notifications.</returns>
    Task<GetUserNotificationsResult> GetUserNotificationsAsync(
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
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="id">The notification ID.</param>
    /// <param name="userId">The user ID (for authorization).</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<MarkAsReadResult> MarkAsReadAsync(Guid id, string userId);

    /// <summary>
    /// Marks all notifications as read for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<MarkAllAsReadResult> MarkAllAsReadAsync(string userId);

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    /// <param name="command">The create notification command.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<CreateNotificationResult> CreateNotificationAsync(CreateNotificationCommand command);
}

/// <summary>
/// Result of the get user notifications operation.
/// </summary>
public class GetUserNotificationsResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the list of notifications.
    /// </summary>
    public IReadOnlyList<Notification> Notifications { get; init; } = [];

    /// <summary>
    /// Gets the total count of notifications matching the filter.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="notifications">The notifications.</param>
    /// <param name="totalCount">The total count.</param>
    /// <param name="page">The current page.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A successful result.</returns>
    public static GetUserNotificationsResult Success(
        IReadOnlyList<Notification> notifications,
        int totalCount,
        int page,
        int pageSize) => new()
    {
        Succeeded = true,
        Notifications = notifications,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetUserNotificationsResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static GetUserNotificationsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}

/// <summary>
/// Result of the mark as read operation.
/// </summary>
public class MarkAsReadResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static MarkAsReadResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static MarkAsReadResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access this notification."]
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static MarkAsReadResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}

/// <summary>
/// Result of the mark all as read operation.
/// </summary>
public class MarkAllAsReadResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the count of notifications marked as read.
    /// </summary>
    public int MarkedCount { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="markedCount">The count of notifications marked as read.</param>
    /// <returns>A successful result.</returns>
    public static MarkAllAsReadResult Success(int markedCount) => new()
    {
        Succeeded = true,
        MarkedCount = markedCount,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static MarkAllAsReadResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}
