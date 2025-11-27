using System.Security.Claims;
using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of seller registration service using ASP.NET Core Identity.
/// </summary>
public class SellerRegistrationService : ISellerRegistrationService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<SellerRegistrationService> _logger;
    private const string BuyerRole = "Buyer";

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerRegistrationService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="logger">The logger instance.</param>
    public SellerRegistrationService(
        UserManager<IdentityUser> userManager,
        ILogger<SellerRegistrationService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RegisterSellerResult> RegisterAsync(RegisterSellerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if email is already registered
        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser != null)
        {
            return RegisterSellerResult.Failure("An account with this email address already exists.");
        }

        // Create the new user
        var user = new IdentityUser
        {
            UserName = command.Email,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return RegisterSellerResult.Failure(errors);
        }

        // Assign the Buyer role (Seller role will be assigned after KYC approval by admin)
        var roleResult = await _userManager.AddToRoleAsync(user, BuyerRole);
        if (!roleResult.Succeeded)
        {
            // If role assignment fails, delete the user to maintain consistency
            await _userManager.DeleteAsync(user);
            return RegisterSellerResult.Failure("Failed to assign buyer role. Please try again.");
        }

        // Store business details as claims
        var claims = new[]
        {
            new Claim("BusinessName", command.BusinessName),
            new Claim("BusinessAddress", command.BusinessAddress),
            new Claim("TaxId", command.TaxId),
            new Claim("ContactName", command.ContactName)
        };

        var claimsResult = await _userManager.AddClaimsAsync(user, claims);
        if (!claimsResult.Succeeded)
        {
            // If claims assignment fails, log warning but continue
            // The seller account is still valid, claims can be added later
            _logger.LogWarning(
                "Failed to add business claims for seller {Email}. Errors: {Errors}",
                command.Email,
                string.Join(", ", claimsResult.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation(
            "New seller registered with email {Email} and business name {BusinessName}.",
            command.Email,
            command.BusinessName);

        return RegisterSellerResult.Success();
    }
}
