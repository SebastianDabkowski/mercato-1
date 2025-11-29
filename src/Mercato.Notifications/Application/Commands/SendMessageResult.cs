namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Result of the send message operation.
/// </summary>
public class SendMessageResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the ID of the sent message.
    /// </summary>
    public Guid? MessageId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="messageId">The ID of the sent message.</param>
    /// <returns>A successful result.</returns>
    public static SendMessageResult Success(Guid messageId) => new()
    {
        Succeeded = true,
        MessageId = messageId,
        Errors = []
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static SendMessageResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to send messages in this thread."]
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static SendMessageResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static SendMessageResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}
