using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Service implementation for handling user account deletion with proper anonymization.
/// </summary>
public class AccountDeletionService : IAccountDeletionService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAccountDeletionCheckService _checkService;
    private readonly IAccountDeletionDataProvider? _dataProvider;
    private readonly ILogger<AccountDeletionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountDeletionService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="checkService">The service for checking blocking conditions.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="dataProvider">Optional provider for deletion operations across modules.</param>
    public AccountDeletionService(
        UserManager<IdentityUser> userManager,
        IAccountDeletionCheckService checkService,
        ILogger<AccountDeletionService> logger,
        IAccountDeletionDataProvider? dataProvider = null)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _checkService = checkService ?? throw new ArgumentNullException(nameof(checkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataProvider = dataProvider;
    }

    /// <inheritdoc/>
    public async Task<AccountDeletionResult> DeleteAccountAsync(string userId, string requestingUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestingUserId);

        // Verify the requesting user is authorized (must be the same user for self-deletion)
        if (!userId.Equals(requestingUserId, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Unauthorized account deletion attempt: User {RequestingUserId} tried to delete account {UserId}",
                requestingUserId,
                userId);
            return AccountDeletionResult.NotAuthorized();
        }

        // Check if user exists
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Account deletion attempted for non-existent user {UserId}", userId);
            return AccountDeletionResult.UserNotFound();
        }

        // Check for blocking conditions
        var checkResult = await _checkService.CheckBlockingConditionsAsync(userId);
        if (!checkResult.CanDelete)
        {
            _logger.LogInformation(
                "Account deletion blocked for user {UserId}: {Conditions}",
                userId,
                string.Join(", ", checkResult.BlockingConditions));
            return AccountDeletionResult.Blocked(checkResult.BlockingConditions);
        }

        var deletedAt = DateTimeOffset.UtcNow;

        try
        {
            // Perform anonymization of related data
            await AnonymizeUserDataAsync(userId);

            // Remove external logins
            var logins = await _userManager.GetLoginsAsync(user);
            foreach (var login in logins)
            {
                var removeResult = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Failed to remove external login {Provider} for user {UserId}: {Errors}",
                        login.LoginProvider,
                        userId,
                        string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                }
            }

            // Remove from all roles
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count > 0)
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, roles);
                if (!removeRolesResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Failed to remove roles for user {UserId}: {Errors}",
                        userId,
                        string.Join(", ", removeRolesResult.Errors.Select(e => e.Description)));
                }
            }

            // Delete the user account
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                var errors = deleteResult.Errors.Select(e => e.Description).ToList();
                _logger.LogError(
                    "Failed to delete user account {UserId}: {Errors}",
                    userId,
                    string.Join(", ", errors));
                return AccountDeletionResult.Failure(errors.AsReadOnly());
            }

            _logger.LogInformation(
                "Account successfully deleted for user {UserId} at {DeletedAt}",
                userId,
                deletedAt);

            return AccountDeletionResult.Success(deletedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account deletion for user {UserId}", userId);
            return AccountDeletionResult.Failure("An unexpected error occurred during account deletion. Please try again.");
        }
    }

    /// <inheritdoc/>
    public async Task<AccountDeletionImpactInfo> GetDeletionImpactAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return AccountDeletionImpactInfo.NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var orderCount = 0;
        var deliveryAddressCount = 0;
        var reviewCount = 0;
        string? storeName = null;

        if (_dataProvider != null)
        {
            orderCount = await _dataProvider.GetOrderCountAsync(userId);
            deliveryAddressCount = await _dataProvider.GetDeliveryAddressCountAsync(userId);
            reviewCount = await _dataProvider.GetReviewCountAsync(userId);
            storeName = await _dataProvider.GetStoreNameAsync(userId);
        }

        return new AccountDeletionImpactInfo
        {
            UserFound = true,
            Email = user.Email,
            Roles = roles.ToList().AsReadOnly(),
            OrderCount = orderCount,
            DeliveryAddressCount = deliveryAddressCount,
            ReviewCount = reviewCount,
            HasStore = !string.IsNullOrEmpty(storeName),
            StoreName = storeName
        };
    }

    /// <summary>
    /// Anonymizes user data across all modules.
    /// </summary>
    private async Task AnonymizeUserDataAsync(string userId)
    {
        if (_dataProvider == null)
        {
            _logger.LogWarning(
                "No data provider configured for anonymization. Skipping data anonymization for user {UserId}.",
                userId);
            return;
        }

        // Anonymize orders (personal data replaced with anonymized values)
        var anonymizedOrders = await _dataProvider.AnonymizeOrderDataAsync(userId);
        _logger.LogInformation(
            "Anonymized {Count} order(s) for user {UserId}",
            anonymizedOrders,
            userId);

        // Delete delivery addresses (no need to retain)
        var deletedAddresses = await _dataProvider.DeleteDeliveryAddressesAsync(userId);
        _logger.LogInformation(
            "Deleted {Count} delivery address(es) for user {UserId}",
            deletedAddresses,
            userId);

        // Anonymize product reviews
        var anonymizedReviews = await _dataProvider.AnonymizeReviewsAsync(userId);
        _logger.LogInformation(
            "Anonymized {Count} product review(s) for user {UserId}",
            anonymizedReviews,
            userId);

        // Anonymize store data if seller
        var storeAnonymized = await _dataProvider.AnonymizeStoreDataAsync(userId);
        if (storeAnonymized)
        {
            _logger.LogInformation("Anonymized store data for seller {UserId}", userId);
        }
    }
}
