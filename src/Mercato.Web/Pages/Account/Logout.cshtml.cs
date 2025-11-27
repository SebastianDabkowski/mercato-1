using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for user logout.
/// Handles both confirmation display and actual logout action.
/// </summary>
public class LogoutModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        SignInManager<IdentityUser> signInManager,
        ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the confirmation prompt.
    /// </summary>
    public bool ShowConfirmation { get; set; }

    public IActionResult OnGet()
    {
        // If user is not authenticated, redirect to login
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Account/Login");
        }

        ShowConfirmation = true;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userEmail = User.Identity.Name;
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {Email} logged out.", userEmail);
        }

        ShowConfirmation = false;
        return Page();
    }
}
