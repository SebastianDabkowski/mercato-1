using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of Facebook OAuth login service for buyers using ASP.NET Core Identity.
/// </summary>
public class FacebookLoginService : IFacebookLoginService
{
    private readonly UserManager<IdentityUser> _userManager;
    private const string BuyerRole = "Buyer";
    private const string FacebookProvider = "Facebook";

    /// <summary>
    /// Initializes a new instance of the <see cref="FacebookLoginService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    public FacebookLoginService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <inheritdoc />
    public async Task<FacebookLoginResult> ProcessFacebookLoginAsync(string email, string facebookId, string? name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(facebookId);

        // Check if user already exists with this email
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            // Verify the user is a buyer
            var isInBuyerRole = await _userManager.IsInRoleAsync(existingUser, BuyerRole);
            if (!isInBuyerRole)
            {
                return FacebookLoginResult.NotABuyer();
            }

            // Check if the Facebook login is already linked
            var logins = await _userManager.GetLoginsAsync(existingUser);
            var hasFacebookLogin = logins.Any(l => l.LoginProvider == FacebookProvider && l.ProviderKey == facebookId);

            if (!hasFacebookLogin)
            {
                // Link Facebook login to existing account
                var loginInfo = new UserLoginInfo(FacebookProvider, facebookId, FacebookProvider);
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, loginInfo);

                if (!addLoginResult.Succeeded)
                {
                    var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                    return FacebookLoginResult.Failure($"Failed to link Facebook account: {errors}");
                }
            }

            return FacebookLoginResult.Success(existingUser.Id, existingUser.Email!);
        }

        // Create new user with Facebook login
        var newUser = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true // Facebook has verified the email
        };

        var createResult = await _userManager.CreateAsync(newUser);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return FacebookLoginResult.Failure($"Failed to create account: {errors}");
        }

        // Add Facebook external login
        var facebookLoginInfo = new UserLoginInfo(FacebookProvider, facebookId, FacebookProvider);
        var externalLoginResult = await _userManager.AddLoginAsync(newUser, facebookLoginInfo);

        if (!externalLoginResult.Succeeded)
        {
            // Rollback: delete the created user if linking fails
            await _userManager.DeleteAsync(newUser);
            var errors = string.Join(", ", externalLoginResult.Errors.Select(e => e.Description));
            return FacebookLoginResult.Failure($"Failed to link Facebook account: {errors}");
        }

        // Assign the Buyer role
        var roleResult = await _userManager.AddToRoleAsync(newUser, BuyerRole);
        if (!roleResult.Succeeded)
        {
            // Rollback: delete the user if role assignment fails
            await _userManager.DeleteAsync(newUser);
            return FacebookLoginResult.Failure("Failed to assign buyer role. Please try again.");
        }

        return FacebookLoginResult.NewUserCreated(newUser.Id, newUser.Email!);
    }
}
