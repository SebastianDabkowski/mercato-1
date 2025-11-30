namespace Mercato.Notifications.Domain.Entities;

/// <summary>
/// Represents a push notification subscription for a user's device.
/// </summary>
public class PushSubscription
{
    /// <summary>
    /// Gets or sets the unique identifier for the subscription.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID (linked to IdentityUser.Id).
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
    /// Gets or sets the date and time when the subscription was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the optional expiration date and time for the subscription.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
