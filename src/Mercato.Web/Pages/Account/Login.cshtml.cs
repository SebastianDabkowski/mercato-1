using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for buyer login.
/// </summary>
public class LoginModel : PageModel
{
    private readonly IBuyerLoginService _loginService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IAuthenticationEventService _authEventService;
    private readonly ILogger<LoginModel> _logger;
    private readonly IConfiguration _configuration;
    private const string BuyerRole = "Buyer";

    public LoginModel(
        IBuyerLoginService loginService,
        SignInManager<IdentityUser> signInManager,
        IAuthenticationEventService authEventService,
        ILogger<LoginModel> logger,
        IConfiguration configuration)
    {
        _loginService = loginService;
        _signInManager = signInManager;
        _authEventService = authEventService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets or sets the login input model.
    /// </summary>
    [BindProperty]
    public LoginBuyerCommand Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the return URL after successful login.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets an external error message (e.g., from Google or Facebook callback).
    /// </summary>
    public string? ExternalErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a success message (e.g., from password change or password reset).
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether Google login is available.
    /// </summary>
    public bool IsGoogleLoginEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether Facebook login is available.
    /// </summary>
    public bool IsFacebookLoginEnabled { get; private set; }

    public void OnGet(string? returnUrl = null, string? errorMessage = null)
    {
        ReturnUrl = returnUrl;
        ExternalErrorMessage = errorMessage ?? TempData["ErrorMessage"]?.ToString();
        StatusMessage = TempData["StatusMessage"]?.ToString();
        
        // Check if Google login is configured
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        IsGoogleLoginEnabled = !string.IsNullOrEmpty(googleClientId);
        
        // Check if Facebook login is configured
        var facebookAppId = _configuration["Authentication:Facebook:AppId"];
        IsFacebookLoginEnabled = !string.IsNullOrEmpty(facebookAppId);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        
        // Re-check configuration for page render
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        IsGoogleLoginEnabled = !string.IsNullOrEmpty(googleClientId);
        
        var facebookAppId = _configuration["Authentication:Facebook:AppId"];
        IsFacebookLoginEnabled = !string.IsNullOrEmpty(facebookAppId);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _loginService.LoginAsync(Input);

        if (result.Succeeded && !string.IsNullOrEmpty(result.UserId))
        {
            // Use the user ID returned from the login service for secure session token creation
            // This avoids an extra database lookup and ensures the session is tied to the validated user
            var user = await _signInManager.UserManager.FindByIdAsync(result.UserId);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: Input.RememberMe);
                _logger.LogInformation("User {Email} logged in successfully.", Input.Email);

                // Log successful login event
                await _authEventService.LogEventAsync(
                    AuthenticationEventType.Login,
                    Input.Email,
                    isSuccessful: true,
                    userId: result.UserId,
                    userRole: BuyerRole,
                    ipAddress: ipAddress,
                    userAgent: userAgent);

                return LocalRedirect(returnUrl);
            }
        }

        // Determine failure reason for logging
        string? failureReason = null;
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", Input.Email);
            failureReason = "Account locked out";

            // Log lockout event
            await _authEventService.LogEventAsync(
                AuthenticationEventType.Lockout,
                Input.Email,
                isSuccessful: false,
                userRole: BuyerRole,
                ipAddress: ipAddress,
                userAgent: userAgent,
                failureReason: failureReason);
        }
        else
        {
            _logger.LogWarning("Invalid login attempt for {Email}.", Input.Email);
            failureReason = result.ErrorMessage ?? "Invalid credentials";

            // Log failed login event
            await _authEventService.LogEventAsync(
                AuthenticationEventType.Login,
                Input.Email,
                isSuccessful: false,
                userRole: BuyerRole,
                ipAddress: ipAddress,
                userAgent: userAgent,
                failureReason: failureReason);
        }

        // Add error to ModelState
        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred during login.");

        return Page();
    }
}
