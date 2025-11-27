using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for initiating Facebook OAuth login for buyers.
/// </summary>
public class FacebookLoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public FacebookLoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        // Check if Facebook authentication is configured
        var appId = _configuration["Authentication:Facebook:AppId"];
        if (string.IsNullOrEmpty(appId))
        {
            TempData["ErrorMessage"] = "Facebook login is not configured.";
            return RedirectToPage("/Account/Login");
        }

        var redirectUrl = Url.Page("/Account/FacebookCallback", pageHandler: null, values: new { returnUrl }, protocol: Request.Scheme);
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        return Challenge(properties, FacebookDefaults.AuthenticationScheme);
    }
}
