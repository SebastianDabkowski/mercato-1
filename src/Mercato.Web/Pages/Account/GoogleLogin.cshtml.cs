using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for initiating Google OAuth login for buyers.
/// </summary>
public class GoogleLoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public GoogleLoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnGet(string? returnUrl = null, bool linkMode = false)
    {
        // Check if Google authentication is configured
        var clientId = _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            TempData["ErrorMessage"] = "Google login is not configured.";
            return RedirectToPage("/Account/Login");
        }

        var redirectUrl = Url.Page("/Account/GoogleCallback", pageHandler: null, values: new { returnUrl, linkMode }, protocol: Request.Scheme);
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }
}
