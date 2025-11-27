using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for handling Google OAuth callback and completing the buyer login process.
/// </summary>
public class GoogleCallbackModel : PageModel
{
    private readonly IGoogleLoginService _googleLoginService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<GoogleCallbackModel> _logger;

    public GoogleCallbackModel(
        IGoogleLoginService googleLoginService,
        SignInManager<IdentityUser> signInManager,
        ILogger<GoogleCallbackModel> logger)
    {
        _googleLoginService = googleLoginService;
        _signInManager = signInManager;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            _logger.LogWarning("Google authentication error: {Error}", remoteError);
            ErrorMessage = $"Error from Google: {remoteError}";
            return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("Could not retrieve external login info from Google.");
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
        }

        // Get user info from the Google claims
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var googleId = info.ProviderKey;
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
        {
            _logger.LogWarning("Google authentication did not provide required claims.");
            ErrorMessage = "Google did not provide the required information. Please try again.";
            return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
        }

        // Process the Google login through our service
        var result = await _googleLoginService.ProcessGoogleLoginAsync(email, googleId, name);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Google login failed for {Email}: {Error}", email, result.ErrorMessage);
            ErrorMessage = result.ErrorMessage ?? "An error occurred during Google login.";
            return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
        }

        // Find the user and sign them in
        var user = await _signInManager.UserManager.FindByIdAsync(result.UserId!);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);

            if (result.IsNewUser)
            {
                _logger.LogInformation("New buyer account created via Google for {Email}.", email);
            }
            else
            {
                _logger.LogInformation("Buyer {Email} logged in via Google.", email);
            }

            return LocalRedirect(returnUrl);
        }

        ErrorMessage = "An error occurred during login. Please try again.";
        return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
    }
}
