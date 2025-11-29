using Mercato.Notifications.Domain.Entities;

namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Command to create a new message thread.
/// </summary>
public class CreateMessageThreadCommand
{
    /// <summary>
    /// Gets or sets the product ID for product-related questions.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the order ID for order-related messaging.
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the subject of the thread.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initial message content.
    /// </summary>
    public string InitialMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the message thread.
    /// </summary>
    public MessageThreadType ThreadType { get; set; }
}
