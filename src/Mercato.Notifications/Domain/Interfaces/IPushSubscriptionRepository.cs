using Mercato.Notifications.Domain.Entities;

namespace Mercato.Notifications.Domain.Interfaces;

/// <summary>
/// Repository interface for push subscription data access operations.
/// </summary>
public interface IPushSubscriptionRepository
{
    /// <summary>
    /// Gets all push subscriptions for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of push subscriptions for the user.</returns>
    Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Adds a new push subscription to the repository.
    /// </summary>
    /// <param name="subscription">The push subscription to add.</param>
    /// <returns>The added push subscription.</returns>
    Task<PushSubscription> AddAsync(PushSubscription subscription);

    /// <summary>
    /// Deletes a push subscription by its unique identifier.
    /// </summary>
    /// <param name="id">The subscription ID.</param>
    /// <returns>True if the subscription was found and deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Deletes all push subscriptions for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The number of subscriptions deleted.</returns>
    Task<int> DeleteByUserIdAsync(string userId);
}
