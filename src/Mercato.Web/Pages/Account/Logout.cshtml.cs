using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
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
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuthenticationEventService _authEventService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IAuthenticationEventService authEventService,
        ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _authEventService = authEventService;
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
            var userId = _userManager.GetUserId(User);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            // Determine user role for logging
            string? userRole = null;
            if (User.IsInRole("Admin"))
            {
                userRole = "Admin";
            }
            else if (User.IsInRole("Seller"))
            {
                userRole = "Seller";
            }
            else if (User.IsInRole("Buyer"))
            {
                userRole = "Buyer";
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {Email} logged out.", userEmail);

            // Log logout event
            await _authEventService.LogEventAsync(
                AuthenticationEventType.Logout,
                userEmail ?? string.Empty,
                isSuccessful: true,
                userId: userId,
                userRole: userRole,
                ipAddress: ipAddress,
                userAgent: userAgent);
        }

        ShowConfirmation = false;
        return Page();
    }
}
