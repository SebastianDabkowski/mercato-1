namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Result of the send push notification operation.
/// </summary>
public class SendPushResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the count of push notifications sent successfully.
    /// </summary>
    public int SentCount { get; init; }

    /// <summary>
    /// Gets the count of push notifications that failed to send.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="sentCount">The count of push notifications sent.</param>
    /// <param name="failedCount">The count of failed push notifications.</param>
    /// <returns>A successful result.</returns>
    public static SendPushResult Success(int sentCount, int failedCount = 0) => new()
    {
        Succeeded = true,
        SentCount = sentCount,
        FailedCount = failedCount,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static SendPushResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static SendPushResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}
