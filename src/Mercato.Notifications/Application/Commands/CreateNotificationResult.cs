namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Result of the create notification operation.
/// </summary>
public class CreateNotificationResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets the ID of the created notification.
    /// </summary>
    public Guid? NotificationId { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="notificationId">The ID of the created notification.</param>
    /// <returns>A successful result.</returns>
    public static CreateNotificationResult Success(Guid notificationId) => new()
    {
        Succeeded = true,
        NotificationId = notificationId,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static CreateNotificationResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failure result with the specified error messages.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>A failure result.</returns>
    public static CreateNotificationResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };
}
