using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of Google OAuth login service for buyers using ASP.NET Core Identity.
/// </summary>
public class GoogleLoginService : IGoogleLoginService
{
    private readonly UserManager<IdentityUser> _userManager;
    private const string BuyerRole = "Buyer";
    private const string GoogleProvider = "Google";

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleLoginService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    public GoogleLoginService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <inheritdoc />
    public async Task<GoogleLoginResult> ProcessGoogleLoginAsync(string email, string googleId, string? name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(googleId);

        // Check if user already exists with this email
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            // Verify the user is a buyer
            var isInBuyerRole = await _userManager.IsInRoleAsync(existingUser, BuyerRole);
            if (!isInBuyerRole)
            {
                return GoogleLoginResult.NotABuyer();
            }

            // Check if the Google login is already linked
            var logins = await _userManager.GetLoginsAsync(existingUser);
            var hasGoogleLogin = logins.Any(l => l.LoginProvider == GoogleProvider && l.ProviderKey == googleId);

            if (!hasGoogleLogin)
            {
                // Link Google login to existing account
                var loginInfo = new UserLoginInfo(GoogleProvider, googleId, GoogleProvider);
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, loginInfo);

                if (!addLoginResult.Succeeded)
                {
                    var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                    return GoogleLoginResult.Failure($"Failed to link Google account: {errors}");
                }
            }

            return GoogleLoginResult.Success(existingUser.Id, existingUser.Email!);
        }

        // Create new user with Google login
        var newUser = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true // Google has verified the email
        };

        var createResult = await _userManager.CreateAsync(newUser);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return GoogleLoginResult.Failure($"Failed to create account: {errors}");
        }

        // Add Google external login
        var googleLoginInfo = new UserLoginInfo(GoogleProvider, googleId, GoogleProvider);
        var externalLoginResult = await _userManager.AddLoginAsync(newUser, googleLoginInfo);

        if (!externalLoginResult.Succeeded)
        {
            // Rollback: delete the created user if linking fails
            await _userManager.DeleteAsync(newUser);
            var errors = string.Join(", ", externalLoginResult.Errors.Select(e => e.Description));
            return GoogleLoginResult.Failure($"Failed to link Google account: {errors}");
        }

        // Assign the Buyer role
        var roleResult = await _userManager.AddToRoleAsync(newUser, BuyerRole);
        if (!roleResult.Succeeded)
        {
            // Rollback: delete the user if role assignment fails
            await _userManager.DeleteAsync(newUser);
            return GoogleLoginResult.Failure("Failed to assign buyer role. Please try again.");
        }

        return GoogleLoginResult.NewUserCreated(newUser.Id, newUser.Email!);
    }
}
