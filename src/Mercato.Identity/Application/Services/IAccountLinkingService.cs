using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for managing linked social accounts for buyers.
/// </summary>
public interface IAccountLinkingService
{
    /// <summary>
    /// Gets all linked social logins for a buyer.
    /// </summary>
    /// <param name="userId">The user ID of the buyer.</param>
    /// <returns>A list of linked account information.</returns>
    Task<IReadOnlyList<LinkedAccountInfo>> GetLinkedAccountsAsync(string userId);

    /// <summary>
    /// Links a social login to an existing buyer account.
    /// </summary>
    /// <param name="userId">The user ID of the buyer.</param>
    /// <param name="provider">The login provider name (e.g., "Google", "Facebook").</param>
    /// <param name="providerKey">The unique identifier from the provider.</param>
    /// <returns>The result of the linking operation.</returns>
    Task<AccountLinkingResult> LinkAccountAsync(string userId, string provider, string providerKey);

    /// <summary>
    /// Unlinks a social login from a buyer account.
    /// </summary>
    /// <param name="userId">The user ID of the buyer.</param>
    /// <param name="provider">The login provider name to unlink.</param>
    /// <returns>The result of the unlinking operation.</returns>
    Task<AccountLinkingResult> UnlinkAccountAsync(string userId, string provider);

    /// <summary>
    /// Checks if a specific provider is linked to a buyer account.
    /// </summary>
    /// <param name="userId">The user ID of the buyer.</param>
    /// <param name="provider">The login provider name.</param>
    /// <returns>True if the provider is linked, false otherwise.</returns>
    Task<bool> IsProviderLinkedAsync(string userId, string provider);
}
