namespace Mercato.Notifications.Domain.Entities;

/// <summary>
/// Represents a conversation thread between a buyer and a seller.
/// </summary>
public class MessageThread
{
    /// <summary>
    /// Gets or sets the unique identifier for the message thread.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID for product-related questions.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the order ID for order-related messaging.
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (linked to IdentityUser.Id).
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller ID (linked to IdentityUser.Id).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID (linked to the seller's store).
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the subject of the thread.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the message thread.
    /// </summary>
    public MessageThreadType ThreadType { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the thread was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the last message in the thread.
    /// </summary>
    public DateTimeOffset LastMessageAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the thread is closed.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the thread was closed.
    /// </summary>
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of messages in this thread.
    /// </summary>
    public ICollection<Message> Messages { get; set; } = [];
}
