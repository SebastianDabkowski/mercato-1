using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of account linking service for managing social logins using ASP.NET Core Identity.
/// </summary>
public class AccountLinkingService : IAccountLinkingService
{
    private readonly UserManager<IdentityUser> _userManager;
    private const string BuyerRole = "Buyer";

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountLinkingService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    public AccountLinkingService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LinkedAccountInfo>> GetLinkedAccountsAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Array.Empty<LinkedAccountInfo>();
        }

        var logins = await _userManager.GetLoginsAsync(user);
        return logins.Select(l => new LinkedAccountInfo
        {
            ProviderName = l.LoginProvider,
            ProviderKey = l.ProviderKey,
            ProviderDisplayName = l.ProviderDisplayName ?? l.LoginProvider
        }).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<AccountLinkingResult> LinkAccountAsync(string userId, string provider, string providerKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return AccountLinkingResult.UserNotFound();
        }

        // Verify the user is a buyer
        var isInBuyerRole = await _userManager.IsInRoleAsync(user, BuyerRole);
        if (!isInBuyerRole)
        {
            return AccountLinkingResult.NotABuyer();
        }

        // Check if the provider is already linked
        var existingLogins = await _userManager.GetLoginsAsync(user);
        if (existingLogins.Any(l => l.LoginProvider == provider))
        {
            return AccountLinkingResult.AlreadyLinked();
        }

        // Link the new provider
        var loginInfo = new UserLoginInfo(provider, providerKey, provider);
        var result = await _userManager.AddLoginAsync(user, loginInfo);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AccountLinkingResult.Failure($"Failed to link {provider} account: {errors}");
        }

        return AccountLinkingResult.Success();
    }

    /// <inheritdoc />
    public async Task<AccountLinkingResult> UnlinkAccountAsync(string userId, string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return AccountLinkingResult.UserNotFound();
        }

        // Verify the user is a buyer
        var isInBuyerRole = await _userManager.IsInRoleAsync(user, BuyerRole);
        if (!isInBuyerRole)
        {
            return AccountLinkingResult.NotABuyer();
        }

        // Find the login to remove
        var existingLogins = await _userManager.GetLoginsAsync(user);
        var loginToRemove = existingLogins.FirstOrDefault(l => l.LoginProvider == provider);

        if (loginToRemove == null)
        {
            return AccountLinkingResult.NotLinked();
        }

        // Ensure user has a password or another login method before unlinking
        var hasPassword = await _userManager.HasPasswordAsync(user);
        var hasOtherLogins = existingLogins.Count > 1;

        if (!hasPassword && !hasOtherLogins)
        {
            return AccountLinkingResult.Failure(
                "Cannot unlink the only login method. Please set a password first or link another social account.");
        }

        // Remove the login
        var result = await _userManager.RemoveLoginAsync(user, loginToRemove.LoginProvider, loginToRemove.ProviderKey);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AccountLinkingResult.Failure($"Failed to unlink {provider} account: {errors}");
        }

        return AccountLinkingResult.Success();
    }

    /// <inheritdoc />
    public async Task<bool> IsProviderLinkedAsync(string userId, string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var logins = await _userManager.GetLoginsAsync(user);
        return logins.Any(l => l.LoginProvider == provider);
    }
}
