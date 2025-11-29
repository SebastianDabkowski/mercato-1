using Mercato.Notifications.Domain.Entities;

namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Command to create a new notification.
/// </summary>
public class CreateNotificationCommand
{
    /// <summary>
    /// Gets or sets the user ID to send the notification to.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of notification.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the optional related entity ID (order, return, etc.).
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Gets or sets the optional URL to navigate when the notification is clicked.
    /// </summary>
    public string? RelatedUrl { get; set; }
}
