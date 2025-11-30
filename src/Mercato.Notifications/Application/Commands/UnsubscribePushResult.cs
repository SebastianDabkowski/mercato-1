namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Result of the unsubscribe push notification operation.
/// </summary>
public class UnsubscribePushResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the count of subscriptions removed.
    /// </summary>
    public int RemovedCount { get; init; }

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
    /// <param name="removedCount">The count of subscriptions removed.</param>
    /// <returns>A successful result.</returns>
    public static UnsubscribePushResult Success(int removedCount) => new()
    {
        Succeeded = true,
        RemovedCount = removedCount,
        Errors = []
    };

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UnsubscribePushResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to perform this operation."]
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static UnsubscribePushResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}
