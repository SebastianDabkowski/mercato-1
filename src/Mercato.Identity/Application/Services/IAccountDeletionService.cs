using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for handling user account deletion with proper anonymization.
/// </summary>
public interface IAccountDeletionService
{
    /// <summary>
    /// Deletes the specified user account with proper data anonymization.
    /// Personal data in orders and financial records is anonymized while preserving
    /// business-critical fields for legal and tax requirements.
    /// </summary>
    /// <param name="userId">The ID of the user requesting deletion.</param>
    /// <param name="requestingUserId">The ID of the user requesting the deletion (for audit purposes).</param>
    /// <returns>The result of the deletion operation.</returns>
    Task<AccountDeletionResult> DeleteAccountAsync(string userId, string requestingUserId);

    /// <summary>
    /// Gets information about what data will be affected by account deletion.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>Information about the deletion impact.</returns>
    Task<AccountDeletionImpactInfo> GetDeletionImpactAsync(string userId);
}

/// <summary>
/// Represents information about the impact of deleting an account.
/// </summary>
public class AccountDeletionImpactInfo
{
    /// <summary>
    /// Gets a value indicating whether the user was found.
    /// </summary>
    public bool UserFound { get; init; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the user's roles.
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>
    /// Gets the count of orders that will have personal data anonymized.
    /// </summary>
    public int OrderCount { get; init; }

    /// <summary>
    /// Gets the count of delivery addresses that will be deleted.
    /// </summary>
    public int DeliveryAddressCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user has a store (seller).
    /// </summary>
    public bool HasStore { get; init; }

    /// <summary>
    /// Gets the store name if the user is a seller.
    /// </summary>
    public string? StoreName { get; init; }

    /// <summary>
    /// Gets the count of product reviews that will be anonymized.
    /// </summary>
    public int ReviewCount { get; init; }

    /// <summary>
    /// Creates an impact info for a user that was not found.
    /// </summary>
    public static AccountDeletionImpactInfo NotFound()
    {
        return new AccountDeletionImpactInfo
        {
            UserFound = false
        };
    }
}
