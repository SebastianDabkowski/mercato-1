using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of admin login service using ASP.NET Core Identity.
/// </summary>
public class AdminLoginService : IAdminLoginService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserBlockCheckService? _userBlockCheckService;
    private const string AdminRole = "Admin";

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminLoginService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="userBlockCheckService">The user block check service (optional).</param>
    public AdminLoginService(
        UserManager<IdentityUser> userManager,
        IUserBlockCheckService? userBlockCheckService = null)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _userBlockCheckService = userBlockCheckService;
    }

    /// <inheritdoc />
    public async Task<LoginAdminResult> LoginAsync(LoginAdminCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Find the user by email
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user == null)
        {
            return LoginAdminResult.InvalidCredentials();
        }

        // Check if the account is blocked (using the block check service if available)
        if (_userBlockCheckService != null)
        {
            var isBlocked = await _userBlockCheckService.IsUserBlockedAsync(user.Id);
            if (isBlocked)
            {
                return LoginAdminResult.Blocked();
            }
        }

        // Check if the account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            return LoginAdminResult.LockedOut();
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
                return LoginAdminResult.LockedOut();
            }
            
            return LoginAdminResult.InvalidCredentials();
        }

        // Check if the user has the Admin role
        var isInAdminRole = await _userManager.IsInRoleAsync(user, AdminRole);
        if (!isInAdminRole)
        {
            return LoginAdminResult.NotAnAdmin();
        }

        // Reset failed access count on successful authentication
        await _userManager.ResetAccessFailedCountAsync(user);

        return LoginAdminResult.Success(user.Id);
    }
}
