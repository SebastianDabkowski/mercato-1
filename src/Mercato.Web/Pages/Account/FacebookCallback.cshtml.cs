using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for handling Facebook OAuth callback and completing the buyer login process.
/// </summary>
public class FacebookCallbackModel : PageModel
{
    private readonly IFacebookLoginService _facebookLoginService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<FacebookCallbackModel> _logger;

    public FacebookCallbackModel(
        IFacebookLoginService facebookLoginService,
        SignInManager<IdentityUser> signInManager,
        ILogger<FacebookCallbackModel> logger)
    {
        _facebookLoginService = facebookLoginService;
        _signInManager = signInManager;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            _logger.LogWarning("Facebook authentication error: {Error}", remoteError);
            ErrorMessage = $"Error from Facebook: {remoteError}";
            return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("Could not retrieve external login info from Facebook.");
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
        }

        // Get user info from the Facebook claims
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var facebookId = info.ProviderKey;
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(facebookId))
        {
            _logger.LogWarning("Facebook authentication did not provide required claims.");
            ErrorMessage = "Facebook did not provide the required information. Please try again.";
            return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
        }

        // Process the Facebook login through our service
        var result = await _facebookLoginService.ProcessFacebookLoginAsync(email, facebookId, name);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Facebook login failed for {Email}: {Error}", email, result.ErrorMessage);
            ErrorMessage = result.ErrorMessage ?? "An error occurred during Facebook login.";
            return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
        }

        // Find the user and sign them in
        var user = await _signInManager.UserManager.FindByIdAsync(result.UserId!);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);

            if (result.IsNewUser)
            {
                _logger.LogInformation("New buyer account created via Facebook for {Email}.", email);
            }
            else
            {
                _logger.LogInformation("Buyer {Email} logged in via Facebook.", email);
            }

            return LocalRedirect(returnUrl);
        }

        ErrorMessage = "An error occurred during login. Please try again.";
        return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
    }
}
