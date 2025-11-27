using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for resetting a user's password using a secure token.
/// </summary>
public class ResetPasswordModel : PageModel
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly IAuthenticationEventService _authEventService;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(
        IPasswordResetService passwordResetService,
        IAuthenticationEventService authEventService,
        ILogger<ResetPasswordModel> logger)
    {
        _passwordResetService = passwordResetService;
        _authEventService = authEventService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the reset password input model.
    /// </summary>
    [BindProperty]
    public ResetPasswordCommand Input { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the token is invalid.
    /// </summary>
    public bool IsInvalidToken { get; set; }

    public IActionResult OnGet(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            // Missing required parameters - show error
            IsInvalidToken = true;
            ModelState.AddModelError(string.Empty, "Invalid password reset link.");
            return Page();
        }

        try
        {
            // Decode the token from Base64Url
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            
            // Pre-populate the email and decoded token
            Input.Email = email;
            Input.Token = decodedToken;
        }
        catch (FormatException)
        {
            // Invalid token format
            IsInvalidToken = true;
            ModelState.AddModelError(string.Empty, "Invalid password reset link.");
            _logger.LogWarning("Invalid token format in password reset link for email: {Email}", email);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _passwordResetService.ResetPasswordAsync(Input);

        if (result.Succeeded)
        {
            _logger.LogInformation("Password successfully reset for {Email}", Input.Email);

            // Log successful password reset event
            await _authEventService.LogEventAsync(
                AuthenticationEventType.PasswordReset,
                Input.Email,
                isSuccessful: true,
                ipAddress: ipAddress,
                userAgent: userAgent);

            return RedirectToPage("/Account/ResetPasswordConfirmation");
        }

        // Determine failure reason for logging
        string? failureReason = null;

        // Handle specific error cases
        if (result.IsInvalidToken || result.IsExpiredToken)
        {
            IsInvalidToken = true;
            failureReason = result.IsExpiredToken ? "Token expired" : "Invalid token";
            _logger.LogWarning("Invalid or expired token used for password reset for {Email}", Input.Email);
        }

        if (result.IsUserNotFound)
        {
            // For security, show a generic error
            failureReason = "User not found";
            _logger.LogWarning("Password reset attempted for non-existent user: {Email}", Input.Email);
        }

        // Log failed password reset event
        await _authEventService.LogEventAsync(
            AuthenticationEventType.PasswordReset,
            Input.Email,
            isSuccessful: false,
            ipAddress: ipAddress,
            userAgent: userAgent,
            failureReason: failureReason);

        // Add errors to ModelState
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return Page();
    }
}
