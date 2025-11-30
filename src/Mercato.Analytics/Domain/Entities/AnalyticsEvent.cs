namespace Mercato.Analytics.Domain.Entities;

/// <summary>
/// Represents an analytics event for tracking user actions.
/// Contains timestamp, user/session identifier, event type, and relevant business identifiers.
/// </summary>
public class AnalyticsEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user ID who performed the action. Null for guest users.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the session identifier for tracking guest users or correlating events.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of event.
    /// </summary>
    public AnalyticsEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the product ID relevant to this event (if applicable).
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID relevant to this event (if applicable).
    /// </summary>
    public int? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the order ID relevant to this event (if applicable).
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the search query for search events (if applicable).
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON for extensibility.
    /// </summary>
    public string? Metadata { get; set; }
}
