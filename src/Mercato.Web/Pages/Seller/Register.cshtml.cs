using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Seller;

/// <summary>
/// Page model for seller registration.
/// </summary>
public class RegisterModel : PageModel
{
    private readonly ISellerRegistrationService _registrationService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        ISellerRegistrationService registrationService,
        SignInManager<IdentityUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _registrationService = registrationService;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the registration input model.
    /// </summary>
    [BindProperty]
    public RegisterSellerCommand Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the return URL after successful registration.
    /// </summary>
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Seller");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _registrationService.RegisterAsync(Input);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "Seller created a new account with email {Email} and business name {BusinessName}.",
                Input.Email,
                Input.BusinessName);

            // Sign in the user after successful registration
            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }

            return LocalRedirect(returnUrl);
        }

        // Add errors to ModelState
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return Page();
    }
}
