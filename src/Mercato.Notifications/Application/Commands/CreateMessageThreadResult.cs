namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Result of the create message thread operation.
/// </summary>
public class CreateMessageThreadResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the ID of the created thread.
    /// </summary>
    public Guid? ThreadId { get; init; }

    /// <summary>
    /// Gets the ID of the initial message.
    /// </summary>
    public Guid? MessageId { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="threadId">The ID of the created thread.</param>
    /// <param name="messageId">The ID of the initial message.</param>
    /// <returns>A successful result.</returns>
    public static CreateMessageThreadResult Success(Guid threadId, Guid messageId) => new()
    {
        Succeeded = true,
        ThreadId = threadId,
        MessageId = messageId,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static CreateMessageThreadResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static CreateMessageThreadResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}
