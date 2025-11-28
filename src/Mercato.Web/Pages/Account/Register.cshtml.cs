using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Services;
using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for buyer registration.
/// </summary>
public class RegisterModel : PageModel
{
    private readonly IBuyerRegistrationService _registrationService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ICartService _cartService;
    private readonly ILogger<RegisterModel> _logger;
    private const string GuestCartCookieName = "GuestCartId";

    public RegisterModel(
        IBuyerRegistrationService registrationService,
        SignInManager<IdentityUser> signInManager,
        ICartService cartService,
        ILogger<RegisterModel> logger)
    {
        _registrationService = registrationService;
        _signInManager = signInManager;
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the registration input model.
    /// </summary>
    [BindProperty]
    public RegisterBuyerCommand Input { get; set; } = new();

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
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _registrationService.RegisterAsync(Input);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with email {Email}.", Input.Email);

            // Sign in the user after successful registration
            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Merge guest cart if exists
                await MergeGuestCartAsync(user.Id);
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

    private async Task MergeGuestCartAsync(string userId)
    {
        var guestCartId = Request.Cookies[GuestCartCookieName];
        if (string.IsNullOrEmpty(guestCartId))
        {
            return;
        }

        try
        {
            var mergeResult = await _cartService.MergeGuestCartAsync(new MergeGuestCartCommand
            {
                BuyerId = userId,
                GuestCartId = guestCartId
            });

            if (mergeResult.Succeeded && mergeResult.GuestCartFound)
            {
                _logger.LogInformation(
                    "Merged {ItemsMerged} items from guest cart to user {UserId}",
                    mergeResult.ItemsMerged, userId);
            }

            // Clear the guest cart cookie after merge attempt
            Response.Cookies.Delete(GuestCartCookieName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging guest cart for user {UserId}", userId);
        }
    }
}
