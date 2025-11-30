using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
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
    private readonly IConsentService _consentService;
    private readonly ILogger<RegisterModel> _logger;
    private const string GuestCartCookieName = "GuestCartId";

    public RegisterModel(
        IBuyerRegistrationService registrationService,
        SignInManager<IdentityUser> signInManager,
        ICartService cartService,
        IConsentService consentService,
        ILogger<RegisterModel> logger)
    {
        _registrationService = registrationService;
        _signInManager = signInManager;
        _cartService = cartService;
        _consentService = consentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the registration input model.
    /// </summary>
    [BindProperty]
    public RegisterBuyerCommand Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the consent selections during registration.
    /// </summary>
    [BindProperty]
    public List<ConsentSelection> ConsentSelections { get; set; } = [];

    /// <summary>
    /// Gets or sets the available consent types for display.
    /// </summary>
    public List<ConsentTypeDto> AvailableConsents { get; set; } = [];

    /// <summary>
    /// Gets or sets the return URL after successful registration.
    /// </summary>
    public string? ReturnUrl { get; set; }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        await LoadConsentTypesAsync();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            await LoadConsentTypesAsync();
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

                // Record user consents
                await RecordUserConsentsAsync(user.Id);

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

        await LoadConsentTypesAsync();
        return Page();
    }

    private async Task LoadConsentTypesAsync()
    {
        var result = await _consentService.GetConsentTypesAsync(new GetConsentTypesQuery());
        if (result.Succeeded)
        {
            AvailableConsents = result.ConsentTypes.ToList();
            
            // Initialize consent selections if not already set
            if (ConsentSelections.Count == 0)
            {
                ConsentSelections = AvailableConsents.Select(c => new ConsentSelection
                {
                    ConsentTypeCode = c.Code,
                    ConsentVersionId = c.CurrentVersionId,
                    IsGranted = false
                }).ToList();
            }
        }
    }

    private async Task RecordUserConsentsAsync(string userId)
    {
        if (ConsentSelections.Count == 0)
        {
            return;
        }

        try
        {
            var command = new RecordMultipleConsentsCommand
            {
                UserId = userId,
                Consents = ConsentSelections.Select(cs => new ConsentDecision
                {
                    ConsentTypeCode = cs.ConsentTypeCode,
                    IsGranted = cs.IsGranted
                }).ToList(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            var result = await _consentService.RecordMultipleConsentsAsync(command);
            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Recorded {Count} consents for user {UserId}",
                    result.ConsentsRecorded, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording consents for user {UserId}", userId);
        }
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

/// <summary>
/// Represents a consent selection during registration.
/// </summary>
public class ConsentSelection
{
    /// <summary>
    /// Gets or sets the consent type code.
    /// </summary>
    public string ConsentTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consent version ID.
    /// </summary>
    public Guid ConsentVersionId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether consent is granted.
    /// </summary>
    public bool IsGranted { get; set; }
}
