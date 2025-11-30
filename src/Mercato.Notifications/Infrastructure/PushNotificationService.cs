using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Mercato.Notifications.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Mercato.Notifications.Infrastructure;

/// <summary>
/// Service implementation for push notification operations.
/// </summary>
public class PushNotificationService : IPushNotificationService
{
    private readonly IPushSubscriptionRepository _pushSubscriptionRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IWebPushClient _webPushClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PushNotificationService"/> class.
    /// </summary>
    /// <param name="pushSubscriptionRepository">The push subscription repository.</param>
    /// <param name="notificationRepository">The notification repository.</param>
    /// <param name="webPushClient">The web push client.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public PushNotificationService(
        IPushSubscriptionRepository pushSubscriptionRepository,
        INotificationRepository notificationRepository,
        IWebPushClient webPushClient,
        IConfiguration configuration,
        ILogger<PushNotificationService> logger)
    {
        _pushSubscriptionRepository = pushSubscriptionRepository;
        _notificationRepository = notificationRepository;
        _webPushClient = webPushClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SubscribePushResult> SubscribeAsync(SubscribePushCommand command)
    {
        var validationErrors = ValidateSubscribeCommand(command);
        if (validationErrors.Count > 0)
        {
            return SubscribePushResult.Failure(validationErrors);
        }

        try
        {
            var subscription = new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                Endpoint = command.Endpoint,
                P256DH = command.P256DH,
                Auth = command.Auth,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = command.ExpiresAt
            };

            await _pushSubscriptionRepository.AddAsync(subscription);

            _logger.LogInformation(
                "Created push subscription {SubscriptionId} for user {UserId}",
                subscription.Id, command.UserId);

            return SubscribePushResult.Success(subscription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating push subscription for user {UserId}", command.UserId);
            return SubscribePushResult.Failure("An error occurred while creating the push subscription.");
        }
    }

    /// <inheritdoc />
    public async Task<UnsubscribePushResult> UnsubscribeAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return UnsubscribePushResult.Failure("User ID is required.");
        }

        try
        {
            var removedCount = await _pushSubscriptionRepository.DeleteByUserIdAsync(userId);

            _logger.LogInformation(
                "Removed {Count} push subscriptions for user {UserId}",
                removedCount, userId);

            return UnsubscribePushResult.Success(removedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing push subscriptions for user {UserId}", userId);
            return UnsubscribePushResult.Failure("An error occurred while removing push subscriptions.");
        }
    }

    /// <inheritdoc />
    public async Task<SendPushResult> SendPushNotificationAsync(Guid notificationId)
    {
        if (notificationId == Guid.Empty)
        {
            return SendPushResult.Failure("Notification ID is required.");
        }

        try
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                return SendPushResult.Failure("Notification not found.");
            }

            var subscriptions = await _pushSubscriptionRepository.GetByUserIdAsync(notification.UserId);
            if (subscriptions.Count == 0)
            {
                _logger.LogDebug(
                    "No push subscriptions found for user {UserId}, notification {NotificationId}",
                    notification.UserId, notificationId);
                return SendPushResult.Success(0);
            }

            var payload = CreatePushPayload(notification);
            var sentCount = 0;
            var failedCount = 0;

            foreach (var subscription in subscriptions)
            {
                try
                {
                    var result = await _webPushClient.SendAsync(
                        subscription.Endpoint,
                        subscription.P256DH,
                        subscription.Auth,
                        payload);

                    if (result.Success)
                    {
                        sentCount++;
                        _logger.LogDebug(
                            "Sent push notification {NotificationId} to subscription {SubscriptionId}",
                            notificationId, subscription.Id);
                    }
                    else
                    {
                        failedCount++;
                        _logger.LogWarning(
                            "Failed to send push notification {NotificationId} to subscription {SubscriptionId}: {Error}",
                            notificationId, subscription.Id, result.ErrorMessage);

                        // If subscription is expired or invalid, delete it
                        if (result.IsSubscriptionGone)
                        {
                            await _pushSubscriptionRepository.DeleteAsync(subscription.Id);
                            _logger.LogInformation(
                                "Removed invalid push subscription {SubscriptionId}",
                                subscription.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogWarning(
                        ex,
                        "Error sending push notification {NotificationId} to subscription {SubscriptionId}",
                        notificationId, subscription.Id);
                }
            }

            _logger.LogInformation(
                "Sent push notification {NotificationId} to {SentCount} subscriptions, {FailedCount} failed",
                notificationId, sentCount, failedCount);

            return SendPushResult.Success(sentCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification {NotificationId}", notificationId);
            return SendPushResult.Failure("An error occurred while sending the push notification.");
        }
    }

    /// <inheritdoc />
    public async Task<GetSubscriptionStatusResult> GetSubscriptionStatusAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return GetSubscriptionStatusResult.Failure("User ID is required.");
        }

        try
        {
            var subscriptions = await _pushSubscriptionRepository.GetByUserIdAsync(userId);
            var activeCount = subscriptions.Count;

            return GetSubscriptionStatusResult.Success(activeCount > 0, activeCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription status for user {UserId}", userId);
            return GetSubscriptionStatusResult.Failure("An error occurred while getting subscription status.");
        }
    }

    private static List<string> ValidateSubscribeCommand(SubscribePushCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(command.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (string.IsNullOrEmpty(command.Endpoint))
        {
            errors.Add("Endpoint is required.");
        }
        else if (command.Endpoint.Length > 2000)
        {
            errors.Add("Endpoint must not exceed 2000 characters.");
        }

        if (string.IsNullOrEmpty(command.P256DH))
        {
            errors.Add("P256DH key is required.");
        }
        else if (command.P256DH.Length > 500)
        {
            errors.Add("P256DH key must not exceed 500 characters.");
        }

        if (string.IsNullOrEmpty(command.Auth))
        {
            errors.Add("Auth secret is required.");
        }
        else if (command.Auth.Length > 500)
        {
            errors.Add("Auth secret must not exceed 500 characters.");
        }

        return errors;
    }

    private static string CreatePushPayload(Notification notification)
    {
        var payload = new
        {
            title = notification.Title,
            body = notification.Message,
            icon = "/images/notification-icon.png",
            badge = "/images/notification-badge.png",
            data = new
            {
                notificationId = notification.Id,
                url = notification.RelatedUrl ?? "/Notifications",
                type = notification.Type.ToString()
            }
        };

        return JsonSerializer.Serialize(payload);
    }
}
