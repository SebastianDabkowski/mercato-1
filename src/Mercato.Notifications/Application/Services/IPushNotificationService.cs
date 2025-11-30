using Mercato.Notifications.Application.Commands;

namespace Mercato.Notifications.Application.Services;

/// <summary>
/// Service interface for push notification operations.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Subscribes a user's device to push notifications.
    /// </summary>
    /// <param name="command">The subscribe push command.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<SubscribePushResult> SubscribeAsync(SubscribePushCommand command);

    /// <summary>
    /// Unsubscribes a user from push notifications by removing all their subscriptions.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<UnsubscribePushResult> UnsubscribeAsync(string userId);

    /// <summary>
    /// Sends a push notification for a given notification ID.
    /// </summary>
    /// <param name="notificationId">The notification ID to send as a push notification.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<SendPushResult> SendPushNotificationAsync(Guid notificationId);

    /// <summary>
    /// Gets the subscription status for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A result containing the subscription status.</returns>
    Task<GetSubscriptionStatusResult> GetSubscriptionStatusAsync(string userId);
}
