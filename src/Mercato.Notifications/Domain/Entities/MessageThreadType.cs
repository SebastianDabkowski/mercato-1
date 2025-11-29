namespace Mercato.Notifications.Domain.Entities;

/// <summary>
/// Represents the type of message thread.
/// </summary>
public enum MessageThreadType
{
    /// <summary>
    /// Thread for product-related questions.
    /// </summary>
    ProductQuestion = 0,

    /// <summary>
    /// Thread for order-related messages.
    /// </summary>
    OrderMessage = 1
}
