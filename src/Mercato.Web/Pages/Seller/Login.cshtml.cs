using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Seller;

/// <summary>
/// Page model for seller login.
/// </summary>
public class LoginModel : PageModel
{
    private readonly ISellerLoginService _loginService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        ISellerLoginService loginService,
        SignInManager<IdentityUser> signInManager,
        ILogger<LoginModel> logger)
    {
        _loginService = loginService;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the login input model.
    /// </summary>
    [BindProperty]
    public LoginSellerCommand Input { get; set; } = new();

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
        returnUrl ??= Url.Content("~/Seller");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _loginService.LoginAsync(Input);

        if (result.Succeeded)
        {
            // Perform the actual sign-in using SignInManager
            // Note: User lookup is needed because the service validates credentials but doesn't return
            // the user object, maintaining separation between validation and ASP.NET Core Identity sign-in
            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: Input.RememberMe);
                _logger.LogInformation("Seller {Email} logged in successfully.", Input.Email);
                return LocalRedirect(returnUrl);
            }
        }

        // Handle specific error cases
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Seller {Email} account locked out.", Input.Email);
        }
        else if (result.EmailNotVerified)
        {
            _logger.LogWarning("Seller {Email} attempted login with unverified email.", Input.Email);
        }
        else
        {
            _logger.LogWarning("Invalid login attempt for seller {Email}.", Input.Email);
        }

        // Add error to ModelState
        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred during login.");

        return Page();
    }
}
