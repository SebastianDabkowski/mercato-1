using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for forgot password functionality.
/// </summary>
public class ForgotPasswordModel : PageModel
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        IPasswordResetService passwordResetService,
        ILogger<ForgotPasswordModel> logger)
    {
        _passwordResetService = passwordResetService;
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

        var result = await _passwordResetService.RequestPasswordResetAsync(Input);

        if (result.Succeeded)
        {
            // Always redirect to confirmation page to prevent email enumeration
            // In production, the token would be sent via email
            _logger.LogInformation("Password reset requested for {Email}", Input.Email);
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
