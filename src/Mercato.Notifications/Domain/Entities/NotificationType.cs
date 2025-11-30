namespace Mercato.Notifications.Domain.Entities;

/// <summary>
/// Represents the type of notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Notification for when an order is placed.
    /// </summary>
    OrderPlaced = 0,

    /// <summary>
    /// Notification for when an order is shipped.
    /// </summary>
    OrderShipped = 1,

    /// <summary>
    /// Notification for when an order is delivered.
    /// </summary>
    OrderDelivered = 2,

    /// <summary>
    /// Notification for when a return is requested.
    /// </summary>
    ReturnRequested = 3,

    /// <summary>
    /// Notification for when a return is approved.
    /// </summary>
    ReturnApproved = 4,

    /// <summary>
    /// Notification for when a return is rejected.
    /// </summary>
    ReturnRejected = 5,

    /// <summary>
    /// Notification for when a payout is processed.
    /// </summary>
    PayoutProcessed = 6,

    /// <summary>
    /// Notification for a message.
    /// </summary>
    Message = 7,

    /// <summary>
    /// Notification for a system update.
    /// </summary>
    SystemUpdate = 8,

    /// <summary>
    /// Notification for when a product is approved by moderation.
    /// </summary>
    ProductApproved = 9,

    /// <summary>
    /// Notification for when a product is rejected by moderation.
    /// </summary>
    ProductRejected = 10,

    /// <summary>
    /// Notification for when a product photo is removed by admin moderation.
    /// </summary>
    PhotoImageRemoved = 11
}
