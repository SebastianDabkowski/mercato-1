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
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        IBuyerLoginService loginService,
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
    public LoginBuyerCommand Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the return URL after successful login.
    /// </summary>
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _loginService.LoginAsync(Input);

        if (result.Succeeded)
        {
            // Perform the actual sign-in using SignInManager
            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: Input.RememberMe);
                _logger.LogInformation("User {Email} logged in successfully.", Input.Email);
                return LocalRedirect(returnUrl);
            }
        }

        // Handle specific error cases
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", Input.Email);
        }
        else
        {
            _logger.LogWarning("Invalid login attempt for {Email}.", Input.Email);
        }

        // Add error to ModelState
        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred during login.");

        return Page();
    }
}
