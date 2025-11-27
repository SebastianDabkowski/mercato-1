using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of buyer registration service using ASP.NET Core Identity.
/// </summary>
public class BuyerRegistrationService : IBuyerRegistrationService
{
    private readonly UserManager<IdentityUser> _userManager;
    private const string BuyerRole = "Buyer";

    /// <summary>
    /// Initializes a new instance of the <see cref="BuyerRegistrationService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    public BuyerRegistrationService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <inheritdoc />
    public async Task<RegisterBuyerResult> RegisterAsync(RegisterBuyerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if email is already registered
        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser != null)
        {
            return RegisterBuyerResult.Failure("An account with this email address already exists.");
        }

        // Create the new user
        var user = new IdentityUser
        {
            UserName = command.Email,
            Email = command.Email
        };

        var result = await _userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return RegisterBuyerResult.Failure(errors);
        }

        // Assign the Buyer role
        var roleResult = await _userManager.AddToRoleAsync(user, BuyerRole);
        if (!roleResult.Succeeded)
        {
            // If role assignment fails, delete the user to maintain consistency
            await _userManager.DeleteAsync(user);
            return RegisterBuyerResult.Failure("Failed to assign buyer role. Please try again.");
        }

        return RegisterBuyerResult.Success();
    }
}
