namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Command to subscribe a user's device to push notifications.
/// </summary>
public class SubscribePushCommand
{
    /// <summary>
    /// Gets or sets the user ID to subscribe.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the push service endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the P256DH encryption key.
    /// </summary>
    public string P256DH { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication secret.
    /// </summary>
    public string Auth { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional expiration date and time for the subscription.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
