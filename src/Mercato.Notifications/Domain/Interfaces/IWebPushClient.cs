namespace Mercato.Notifications.Domain.Interfaces;

/// <summary>
/// Interface for web push client operations.
/// </summary>
public interface IWebPushClient
{
    /// <summary>
    /// Sends a push notification to a subscription endpoint.
    /// </summary>
    /// <param name="endpoint">The push subscription endpoint URL.</param>
    /// <param name="p256dh">The P256DH encryption key.</param>
    /// <param name="auth">The authentication secret.</param>
    /// <param name="payload">The notification payload as JSON string.</param>
    /// <returns>A result indicating success or failure, with error details if applicable.</returns>
    Task<WebPushSendResult> SendAsync(string endpoint, string p256dh, string auth, string payload);
}

/// <summary>
/// Result of a web push send operation.
/// </summary>
public class WebPushSendResult
{
    /// <summary>
    /// Gets a value indicating whether the push was sent successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets a value indicating whether the subscription is expired or invalid.
    /// </summary>
    public bool IsSubscriptionGone { get; init; }

    /// <summary>
    /// Gets the error message if the push failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static WebPushSendResult Succeeded() => new() { Success = true };

    /// <summary>
    /// Creates a failed result with the subscription marked as gone/expired.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failure result.</returns>
    public static WebPushSendResult SubscriptionGone(string? errorMessage = null) => new()
    {
        Success = false,
        IsSubscriptionGone = true,
        ErrorMessage = errorMessage ?? "Subscription is expired or invalid."
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failure result.</returns>
    public static WebPushSendResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
