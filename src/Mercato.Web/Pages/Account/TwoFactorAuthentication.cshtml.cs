using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for the Two-Factor Authentication settings page.
/// Displays information about the upcoming 2FA feature.
/// Available to both Buyers and Sellers.
/// </summary>
[Authorize]
public class TwoFactorAuthenticationModel : PageModel
{
    /// <summary>
    /// Gets a value indicating whether two-factor authentication is available.
    /// Currently always returns false as 2FA is planned for a future release.
    /// </summary>
    public bool IsTwoFactorAvailable => false;

    /// <summary>
    /// Gets a value indicating whether two-factor authentication is enabled for the current user.
    /// Currently always returns false as 2FA is not yet implemented.
    /// </summary>
    public bool IsTwoFactorEnabled => false;

    public void OnGet()
    {
        // No action needed for GET request
        // This page displays static information about the upcoming 2FA feature
    }
}
