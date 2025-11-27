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
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(
        IPasswordResetService passwordResetService,
        ILogger<ResetPasswordModel> logger)
    {
        _passwordResetService = passwordResetService;
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

        var result = await _passwordResetService.ResetPasswordAsync(Input);

        if (result.Succeeded)
        {
            _logger.LogInformation("Password successfully reset for {Email}", Input.Email);
            return RedirectToPage("/Account/ResetPasswordConfirmation");
        }

        // Handle specific error cases
        if (result.IsInvalidToken || result.IsExpiredToken)
        {
            IsInvalidToken = true;
            _logger.LogWarning("Invalid or expired token used for password reset for {Email}", Input.Email);
        }

        if (result.IsUserNotFound)
        {
            // For security, show a generic error
            _logger.LogWarning("Password reset attempted for non-existent user: {Email}", Input.Email);
        }

        // Add errors to ModelState
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return Page();
    }
}
