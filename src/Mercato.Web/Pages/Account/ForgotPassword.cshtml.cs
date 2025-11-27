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
/// Page model for forgot password functionality.
/// </summary>
public class ForgotPasswordModel : PageModel
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly IAuthenticationEventService _authEventService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        IPasswordResetService passwordResetService,
        IAuthenticationEventService authEventService,
        ILogger<ForgotPasswordModel> logger)
    {
        _passwordResetService = passwordResetService;
        _authEventService = authEventService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the forgot password input model.
    /// </summary>
    [BindProperty]
    public ForgotPasswordCommand Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _passwordResetService.RequestPasswordResetAsync(Input);

        // Log all password reset requests for security monitoring
        // (regardless of whether user exists, to help detect account enumeration attempts)
        await _authEventService.LogEventAsync(
            AuthenticationEventType.PasswordReset,
            Input.Email,
            isSuccessful: result.Succeeded,
            ipAddress: ipAddress,
            userAgent: userAgent);

        if (result.Succeeded)
        {
            // Always redirect to confirmation page to prevent email enumeration
            _logger.LogInformation("Password reset requested for {Email}", Input.Email);

            // Build the reset link if token was generated (user exists)
            if (!string.IsNullOrEmpty(result.ResetToken))
            {
                // Encode the token for URL safety
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(result.ResetToken));
                
                // Build the password reset URL for email integration
                // TODO: Implement email sending service to send the reset link
                // Example: await _emailService.SendPasswordResetEmailAsync(Input.Email, resetLink);
#pragma warning disable IDE0059 // Unnecessary assignment - kept for future email integration
                var resetLink = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { email = Input.Email, token = encodedToken },
                    protocol: Request.Scheme);
#pragma warning restore IDE0059
                
                // Log that a link was generated (without exposing the actual token for security)
                _logger.LogInformation("Password reset link generated for user with email: {Email}", Input.Email);
            }

            return RedirectToPage("/Account/ForgotPasswordConfirmation");
        }

        // Only show error for unexpected failures (not for "email not found")
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }

        return Page();
    }
}
