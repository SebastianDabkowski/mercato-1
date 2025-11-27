using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Implementation of password reset service using ASP.NET Core Identity.
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<PasswordResetService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordResetService"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="logger">The logger instance.</param>
    public PasswordResetService(
        UserManager<IdentityUser> userManager,
        ILogger<PasswordResetService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ForgotPasswordResult> RequestPasswordResetAsync(ForgotPasswordCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Log the password reset request for audit purposes
        _logger.LogInformation("Password reset requested for email: {Email}", command.Email);

        // Find the user by email
        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user == null)
        {
            // For security reasons, don't reveal if the email exists or not
            // Log for audit purposes without revealing the email to protect privacy
            _logger.LogInformation("Password reset requested for email that was not found in the system.");
            return ForgotPasswordResult.EmailNotFound();
        }

        try
        {
            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Log successful token generation for audit purposes
            _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);

            // In a production environment, this token would be sent via email
            // For now, we return it in the result (to be used by the caller for sending email)
            return ForgotPasswordResult.Success(token, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for email: {Email}", command.Email);
            return ForgotPasswordResult.Failure("An unexpected error occurred. Please try again later.");
        }
    }

    /// <inheritdoc />
    public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Log the password reset attempt for audit purposes
        _logger.LogInformation("Password reset attempt for email: {Email}", command.Email);

        // Find the user by email
        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user == null)
        {
            // For security, log but don't reveal if user exists
            _logger.LogWarning("Password reset attempted for non-existent email: {Email}", command.Email);
            return ResetPasswordResult.UserNotFound();
        }

        try
        {
            // Attempt to reset the password using Identity's built-in token validation
            // This handles token expiry and single-use validation internally
            var result = await _userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);

            if (result.Succeeded)
            {
                // Log successful password reset for audit purposes
                _logger.LogInformation("Password reset successful for user: {UserId}", user.Id);
                return ResetPasswordResult.Success();
            }

            // Check for specific error types
            var errors = result.Errors.ToList();
            
            // Identity returns "InvalidToken" error code for invalid or expired tokens
            if (errors.Any(e => e.Code == "InvalidToken"))
            {
                _logger.LogWarning("Invalid or expired token used for password reset for user: {UserId}", user.Id);
                return ResetPasswordResult.InvalidToken();
            }

            // Log the failure for audit purposes
            _logger.LogWarning("Password reset failed for user: {UserId}. Errors: {Errors}", 
                user.Id, 
                string.Join(", ", errors.Select(e => e.Description)));

            return ResetPasswordResult.Failure(errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for email: {Email}", command.Email);
            return ResetPasswordResult.Failure("An unexpected error occurred. Please try again later.");
        }
    }
}
