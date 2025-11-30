using System.Text.Json;
using System.Text.Json.Serialization;
using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Queries;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of user data export service for GDPR compliance.
/// </summary>
public class UserDataExportService : IUserDataExportService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserDataProvider? _userDataProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataExportService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="userDataProvider">Optional provider for additional user data from other modules.</param>
    public UserDataExportService(
        UserManager<IdentityUser> userManager,
        IUserDataProvider? userDataProvider = null)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _userDataProvider = userDataProvider;
    }

    /// <inheritdoc />
    public async Task<UserDataExportResult> ExportUserDataAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return UserDataExportResult.UserNotFound();
        }

        var exportedAt = DateTimeOffset.UtcNow;

        var export = new UserDataExport
        {
            ExportedAt = exportedAt,
            Identity = await BuildIdentityDataAsync(user)
        };

        // Add additional data from other modules if provider is available
        if (_userDataProvider != null)
        {
            export.DeliveryAddresses = await _userDataProvider.GetDeliveryAddressesAsync(userId);
            export.Orders = await _userDataProvider.GetOrdersAsync(userId);
            export.Store = await _userDataProvider.GetStoreAsync(userId);
            export.Consents = await _userDataProvider.GetConsentsAsync(userId);
        }

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var exportData = JsonSerializer.Serialize(export, jsonOptions);

        return UserDataExportResult.Success(exportData, exportedAt);
    }

    private async Task<UserIdentityData> BuildIdentityDataAsync(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var logins = await _userManager.GetLoginsAsync(user);

        return new UserIdentityData
        {
            UserId = user.Id,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList(),
            TwoFactorEnabled = user.TwoFactorEnabled,
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            LockoutEnd = user.LockoutEnd,
            ExternalLogins = logins.Select(l => new ExternalLoginData
            {
                ProviderName = l.LoginProvider,
                ProviderDisplayName = l.ProviderDisplayName
            }).ToList()
        };
    }
}
