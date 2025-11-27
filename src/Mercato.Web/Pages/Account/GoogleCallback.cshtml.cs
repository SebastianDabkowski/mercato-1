using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
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
    private readonly IAccountLinkingService _accountLinkingService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IAuthenticationEventService _authEventService;
    private readonly ILogger<GoogleCallbackModel> _logger;
    private const string BuyerRole = "Buyer";

    public GoogleCallbackModel(
        IGoogleLoginService googleLoginService,
        IAccountLinkingService accountLinkingService,
        SignInManager<IdentityUser> signInManager,
        IAuthenticationEventService authEventService,
        ILogger<GoogleCallbackModel> logger)
    {
        _googleLoginService = googleLoginService;
        _accountLinkingService = accountLinkingService;
        _signInManager = signInManager;
        _authEventService = authEventService;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null, string? remoteError = null, bool linkMode = false)
    {
        returnUrl ??= Url.Content("~/");
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

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

        // Handle link mode - user is already authenticated and wants to link Google account
        if (linkMode && User.Identity?.IsAuthenticated == true)
        {
            return await HandleLinkModeAsync(googleId, returnUrl);
        }

        // Process the Google login through our service
        var result = await _googleLoginService.ProcessGoogleLoginAsync(email, googleId, name);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Google login failed for {Email}: {Error}", email, result.ErrorMessage);
            ErrorMessage = result.ErrorMessage ?? "An error occurred during Google login.";

            // Log failed Google login event
            await _authEventService.LogEventAsync(
                AuthenticationEventType.Login,
                email,
                isSuccessful: false,
                userRole: BuyerRole,
                ipAddress: ipAddress,
                userAgent: userAgent,
                failureReason: $"Google OAuth: {result.ErrorMessage}");

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

            // Log successful Google login event
            await _authEventService.LogEventAsync(
                AuthenticationEventType.Login,
                email,
                isSuccessful: true,
                userId: result.UserId,
                userRole: BuyerRole,
                ipAddress: ipAddress,
                userAgent: userAgent);

            return LocalRedirect(returnUrl);
        }

        ErrorMessage = "An error occurred during login. Please try again.";
        return RedirectToPage("/Account/Login", new { errorMessage = ErrorMessage });
    }

    private async Task<IActionResult> HandleLinkModeAsync(string googleId, string returnUrl)
    {
        var userId = _signInManager.UserManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var linkResult = await _accountLinkingService.LinkAccountAsync(userId, "Google", googleId);

        if (linkResult.Succeeded)
        {
            if (linkResult.WasAlreadyLinked)
            {
                _logger.LogInformation("User {UserId} attempted to link Google account but it was already linked.", userId);
                TempData["StatusMessage"] = "Your Google account is already linked.";
            }
            else
            {
                _logger.LogInformation("User {UserId} successfully linked Google account.", userId);
                TempData["StatusMessage"] = "Successfully linked your Google account.";
            }
            TempData["IsError"] = false;
        }
        else
        {
            _logger.LogWarning("Failed to link Google account for user {UserId}: {Error}", userId, linkResult.ErrorMessage);
            TempData["StatusMessage"] = linkResult.ErrorMessage ?? "Failed to link Google account.";
            TempData["IsError"] = true;
        }

        return RedirectToPage("/Account/ManageLinkedAccounts");
    }
}
