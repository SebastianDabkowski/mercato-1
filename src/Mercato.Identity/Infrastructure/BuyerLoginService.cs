using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of buyer login service using ASP.NET Core Identity.
/// </summary>
public class BuyerLoginService : IBuyerLoginService
{
    private readonly UserManager<IdentityUser> _userManager;
    private const string BuyerRole = "Buyer";

    /// <summary>
    /// Initializes a new instance of the <see cref="BuyerLoginService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    public BuyerLoginService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <inheritdoc />
    public async Task<LoginBuyerResult> LoginAsync(LoginBuyerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Find the user by email
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user == null)
        {
            return LoginBuyerResult.InvalidCredentials();
        }

        // Check if the account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            return LoginBuyerResult.LockedOut();
        }

        // Verify the password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!isPasswordValid)
        {
            // Increment failed access count
            await _userManager.AccessFailedAsync(user);
            
            // Check if the user is now locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                return LoginBuyerResult.LockedOut();
            }
            
            return LoginBuyerResult.InvalidCredentials();
        }

        // Check if the user has the Buyer role
        var isInBuyerRole = await _userManager.IsInRoleAsync(user, BuyerRole);
        if (!isInBuyerRole)
        {
            return LoginBuyerResult.NotABuyer();
        }

        // Reset failed access count on successful authentication
        await _userManager.ResetAccessFailedCountAsync(user);

        return LoginBuyerResult.Success();
    }
}
