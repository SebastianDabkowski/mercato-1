namespace Mercato.Notifications.Domain.Entities;

/// <summary>
/// Represents an individual message in a message thread.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the thread ID (foreign key to MessageThread).
    /// </summary>
    public Guid ThreadId { get; set; }

    /// <summary>
    /// Gets or sets the sender ID (linked to IdentityUser.Id).
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the message was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message has been read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was read.
    /// </summary>
    public DateTimeOffset? ReadAt { get; set; }

    /// <summary>
    /// Gets or sets the parent message thread.
    /// </summary>
    public MessageThread? Thread { get; set; }
}
