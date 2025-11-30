using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of seller login service using ASP.NET Core Identity.
/// </summary>
public class SellerLoginService : ISellerLoginService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserBlockCheckService? _userBlockCheckService;
    private const string SellerRole = "Seller";

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerLoginService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="userBlockCheckService">The user block check service (optional).</param>
    public SellerLoginService(
        UserManager<IdentityUser> userManager,
        IUserBlockCheckService? userBlockCheckService = null)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _userBlockCheckService = userBlockCheckService;
    }

    /// <inheritdoc />
    public async Task<LoginSellerResult> LoginAsync(LoginSellerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Find the user by email
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user == null)
        {
            return LoginSellerResult.InvalidCredentials();
        }

        // Check if the account is blocked (using the block check service if available)
        if (_userBlockCheckService != null)
        {
            var isBlocked = await _userBlockCheckService.IsUserBlockedAsync(user.Id);
            if (isBlocked)
            {
                return LoginSellerResult.Blocked();
            }
        }

        // Check if the account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            return LoginSellerResult.LockedOut();
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
                return LoginSellerResult.LockedOut();
            }
            
            return LoginSellerResult.InvalidCredentials();
        }

        // Check if the user has the Seller role
        var isInSellerRole = await _userManager.IsInRoleAsync(user, SellerRole);
        if (!isInSellerRole)
        {
            return LoginSellerResult.NotASeller();
        }

        // Check if email is verified
        var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        if (!isEmailConfirmed)
        {
            return LoginSellerResult.UnverifiedEmail();
        }

        // Reset failed access count on successful authentication
        await _userManager.ResetAccessFailedCountAsync(user);

        return LoginSellerResult.Success(user.Id);
    }
}
