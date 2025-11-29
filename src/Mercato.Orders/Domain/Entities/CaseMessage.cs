namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a message in a case (return request) messaging thread.
/// </summary>
public class CaseMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the return request ID this message belongs to.
    /// </summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the message sender.
    /// </summary>
    public string SenderUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the sender (Buyer, Seller, Admin).
    /// </summary>
    public string SenderRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content (text only).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the message was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent return request.
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;
}
