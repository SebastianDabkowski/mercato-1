namespace Mercato.Identity.Application.Services;

/// <summary>
/// Interface for providing data operations needed for account deletion and anonymization.
/// Implemented by the web application to coordinate data from multiple modules.
/// </summary>
public interface IAccountDeletionDataProvider
{
    /// <summary>
    /// Gets the count of orders for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of orders.</returns>
    Task<int> GetOrderCountAsync(string userId);

    /// <summary>
    /// Gets the count of delivery addresses for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of delivery addresses.</returns>
    Task<int> GetDeliveryAddressCountAsync(string userId);

    /// <summary>
    /// Gets the count of product reviews for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of product reviews.</returns>
    Task<int> GetReviewCountAsync(string userId);

    /// <summary>
    /// Gets the store name for the specified seller user, if they have a store.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The store name if found; otherwise, null.</returns>
    Task<string?> GetStoreNameAsync(string userId);

    /// <summary>
    /// Gets the count of open disputes (return requests under review) for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of open disputes.</returns>
    Task<int> GetOpenDisputeCountAsync(string userId);

    /// <summary>
    /// Gets the count of pending refunds for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of pending refunds.</returns>
    Task<int> GetPendingRefundCountAsync(string userId);

    /// <summary>
    /// Anonymizes order data for the specified user.
    /// Replaces personal data (name, email, phone, address) with anonymized values
    /// while preserving business-critical fields (amounts, dates, product IDs).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of orders anonymized.</returns>
    Task<int> AnonymizeOrderDataAsync(string userId);

    /// <summary>
    /// Deletes all delivery addresses for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of addresses deleted.</returns>
    Task<int> DeleteDeliveryAddressesAsync(string userId);

    /// <summary>
    /// Anonymizes product reviews for the specified user.
    /// Sets buyer display name to "Deleted User" while preserving review content.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The count of reviews anonymized.</returns>
    Task<int> AnonymizeReviewsAsync(string userId);

    /// <summary>
    /// Anonymizes store data for the specified seller user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if a store was anonymized; otherwise, false.</returns>
    Task<bool> AnonymizeStoreDataAsync(string userId);
}
