using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Notifications.Infrastructure;

/// <summary>
/// Service implementation for notification operations.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="notificationRepository">The notification repository.</param>
    /// <param name="logger">The logger.</param>
    public NotificationService(
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetUserNotificationsResult> GetUserNotificationsAsync(
        string userId,
        bool? isRead,
        int page,
        int pageSize)
    {
        var validationErrors = ValidateGetNotificationsQuery(userId, page, pageSize);
        if (validationErrors.Count > 0)
        {
            return GetUserNotificationsResult.Failure(validationErrors);
        }

        try
        {
            var (notifications, totalCount) = await _notificationRepository.GetByUserIdAsync(
                userId,
                isRead,
                page,
                pageSize);

            return GetUserNotificationsResult.Success(notifications, totalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return GetUserNotificationsResult.Failure("An error occurred while getting the notifications.");
        }
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return 0;
        }

        try
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count for user {UserId}", userId);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<MarkAsReadResult> MarkAsReadAsync(Guid id, string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return MarkAsReadResult.Failure("User ID is required.");
        }

        if (id == Guid.Empty)
        {
            return MarkAsReadResult.Failure("Notification ID is required.");
        }

        try
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
            {
                return MarkAsReadResult.Failure("Notification not found.");
            }

            if (notification.UserId != userId)
            {
                return MarkAsReadResult.NotAuthorized();
            }

            var success = await _notificationRepository.MarkAsReadAsync(id, userId);
            if (!success)
            {
                return MarkAsReadResult.Failure("Failed to mark notification as read.");
            }

            _logger.LogDebug("Marked notification {NotificationId} as read for user {UserId}", id, userId);
            return MarkAsReadResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return MarkAsReadResult.Failure("An error occurred while marking the notification as read.");
        }
    }

    /// <inheritdoc />
    public async Task<MarkAllAsReadResult> MarkAllAsReadAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return MarkAllAsReadResult.Failure("User ID is required.");
        }

        try
        {
            var count = await _notificationRepository.MarkAllAsReadAsync(userId);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, userId);
            return MarkAllAsReadResult.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return MarkAllAsReadResult.Failure("An error occurred while marking all notifications as read.");
        }
    }

    /// <inheritdoc />
    public async Task<CreateNotificationResult> CreateNotificationAsync(CreateNotificationCommand command)
    {
        var validationErrors = ValidateCreateCommand(command);
        if (validationErrors.Count > 0)
        {
            return CreateNotificationResult.Failure(validationErrors);
        }

        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                Title = command.Title,
                Message = command.Message,
                Type = command.Type,
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow,
                RelatedEntityId = command.RelatedEntityId,
                RelatedUrl = command.RelatedUrl
            };

            await _notificationRepository.AddAsync(notification);

            _logger.LogInformation(
                "Created notification {NotificationId} of type {Type} for user {UserId}",
                notification.Id, command.Type, command.UserId);

            return CreateNotificationResult.Success(notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}", command.UserId);
            return CreateNotificationResult.Failure("An error occurred while creating the notification.");
        }
    }

    private static List<string> ValidateGetNotificationsQuery(string userId, int page, int pageSize)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(userId))
        {
            errors.Add("User ID is required.");
        }

        if (page < 1)
        {
            errors.Add("Page number must be at least 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            errors.Add("Page size must be between 1 and 100.");
        }

        return errors;
    }

    private static List<string> ValidateCreateCommand(CreateNotificationCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (string.IsNullOrEmpty(command.Title))
        {
            errors.Add("Title is required.");
        }
        else if (command.Title.Length > 200)
        {
            errors.Add("Title must not exceed 200 characters.");
        }

        if (string.IsNullOrEmpty(command.Message))
        {
            errors.Add("Message is required.");
        }
        else if (command.Message.Length > 2000)
        {
            errors.Add("Message must not exceed 2000 characters.");
        }

        if (!string.IsNullOrEmpty(command.RelatedUrl) && command.RelatedUrl.Length > 500)
        {
            errors.Add("Related URL must not exceed 500 characters.");
        }

        return errors;
    }
}
