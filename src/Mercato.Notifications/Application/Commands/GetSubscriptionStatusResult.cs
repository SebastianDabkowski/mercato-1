namespace Mercato.Notifications.Application.Commands;

/// <summary>
/// Result of the get subscription status operation.
/// </summary>
public class GetSubscriptionStatusResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user has an active push subscription.
    /// </summary>
    public bool IsSubscribed { get; init; }

    /// <summary>
    /// Gets the count of active subscriptions for the user.
    /// </summary>
    public int SubscriptionCount { get; init; }

    /// <summary>
    /// Gets the list of error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="isSubscribed">Whether the user has an active subscription.</param>
    /// <param name="subscriptionCount">The count of active subscriptions.</param>
    /// <returns>A successful result.</returns>
    public static GetSubscriptionStatusResult Success(bool isSubscribed, int subscriptionCount) => new()
    {
        Succeeded = true,
        IsSubscribed = isSubscribed,
        SubscriptionCount = subscriptionCount,
        Errors = []
    };

    /// <summary>
    /// Creates a failure result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static GetSubscriptionStatusResult Failure(string error) => new()
    {
        Succeeded = false,
        Errors = [error]
    };
}
