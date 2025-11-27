using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of password change service using ASP.NET Core Identity.
/// </summary>
public class PasswordChangeService : IPasswordChangeService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<PasswordChangeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordChangeService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="logger">The logger instance.</param>
    public PasswordChangeService(
        UserManager<IdentityUser> userManager,
        ILogger<PasswordChangeService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Log the password change attempt for audit purposes
        _logger.LogInformation("Password change attempt for user: {UserId}", command.UserId);

        // Find the user by ID
        var user = await _userManager.FindByIdAsync(command.UserId);

        if (user == null)
        {
            _logger.LogWarning("Password change attempted for non-existent user: {UserId}", command.UserId);
            return ChangePasswordResult.UserNotFound();
        }

        try
        {
            // Validate the new password matches confirmation (belt and suspenders - should be done at UI level too)
            if (command.NewPassword != command.ConfirmPassword)
            {
                _logger.LogWarning("Password change failed for user: {UserId}. Password confirmation does not match.", command.UserId);
                return ChangePasswordResult.Failure("The new password and confirmation password do not match.");
            }

            // Attempt to change the password using Identity's built-in method
            // This validates the current password and applies password policies
            var result = await _userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);

            if (result.Succeeded)
            {
                // Log successful password change for audit purposes
                _logger.LogInformation("Password changed successfully for user: {UserId}", command.UserId);

                // Update the security stamp to invalidate other sessions
                // This ensures that other active sessions will be logged out for security
                await _userManager.UpdateSecurityStampAsync(user);

                return ChangePasswordResult.Success();
            }

            // Check for specific error types
            var errors = result.Errors.ToList();

            // Check if the current password was incorrect
            if (errors.Any(e => e.Code == "PasswordMismatch"))
            {
                _logger.LogWarning("Incorrect current password provided for user: {UserId}", command.UserId);
                return ChangePasswordResult.IncorrectCurrentPassword();
            }

            // Log the failure for audit purposes
            _logger.LogWarning("Password change failed for user: {UserId}. Errors: {Errors}",
                command.UserId,
                string.Join(", ", errors.Select(e => e.Description)));

            return ChangePasswordResult.Failure(errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change for user: {UserId}", command.UserId);
            return ChangePasswordResult.Failure("An unexpected error occurred. Please try again later.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasPasswordAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        return await _userManager.HasPasswordAsync(user);
    }
}
