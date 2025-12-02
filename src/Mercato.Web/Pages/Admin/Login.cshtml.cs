using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin;

/// <summary>
/// Page model for admin login.
/// </summary>
public class LoginModel : PageModel
{
    private readonly IAdminLoginService _loginService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IAuthenticationEventService _authEventService;
    private readonly ILogger<LoginModel> _logger;
    private const string AdminRole = "Admin";

    public LoginModel(
        IAdminLoginService loginService,
        SignInManager<IdentityUser> signInManager,
        IAuthenticationEventService authEventService,
        ILogger<LoginModel> logger)
    {
        _loginService = loginService;
        _signInManager = signInManager;
        _authEventService = authEventService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the login input model.
    /// </summary>
    [BindProperty]
    public LoginAdminCommand Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the return URL after successful login.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets an error message (e.g., from redirect).
    /// </summary>
    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null, string? errorMessage = null)
    {
        ReturnUrl = returnUrl;
        ErrorMessage = errorMessage ?? TempData["ErrorMessage"]?.ToString();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Admin");

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
                _logger.LogInformation("Admin {Email} logged in successfully.", Input.Email);

                // Log successful login event
                await _authEventService.LogEventAsync(
                    AuthenticationEventType.Login,
                    Input.Email,
                    isSuccessful: true,
                    userId: result.UserId,
                    userRole: AdminRole,
                    ipAddress: ipAddress,
                    userAgent: userAgent);

                return LocalRedirect(returnUrl);
            }
        }

        // Determine failure reason for logging
        string? failureReason = null;
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Admin {Email} account locked out.", Input.Email);
            failureReason = "Account locked out";

            // Log lockout event
            await _authEventService.LogEventAsync(
                AuthenticationEventType.Lockout,
                Input.Email,
                isSuccessful: false,
                userRole: AdminRole,
                ipAddress: ipAddress,
                userAgent: userAgent,
                failureReason: failureReason);
        }
        else
        {
            _logger.LogWarning("Invalid login attempt for admin {Email}.", Input.Email);
            failureReason = result.ErrorMessage ?? "Invalid credentials";

            // Log failed login event
            await _authEventService.LogEventAsync(
                AuthenticationEventType.Login,
                Input.Email,
                isSuccessful: false,
                userRole: AdminRole,
                ipAddress: ipAddress,
                userAgent: userAgent,
                failureReason: failureReason);
        }

        // Add error to ModelState
        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred during login.");

        return Page();
    }
}
