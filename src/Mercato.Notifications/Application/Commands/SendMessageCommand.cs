namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Command to send a message in an existing thread.
/// </summary>
public class SendMessageCommand
{
    /// <summary>
    /// Gets or sets the thread ID.
    /// </summary>
    public Guid ThreadId { get; set; }

    /// <summary>
    /// Gets or sets the sender ID.
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
