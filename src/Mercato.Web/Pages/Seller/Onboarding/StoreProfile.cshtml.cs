using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Onboarding;

/// <summary>
/// Page model for the store profile step of the seller onboarding wizard.
/// </summary>
[Authorize(Roles = "Buyer,Seller")]
public class StoreProfileModel : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;
    private readonly ILogger<StoreProfileModel> _logger;

    public StoreProfileModel(
        ISellerOnboardingService onboardingService,
        ILogger<StoreProfileModel> logger)
    {
        _onboardingService = onboardingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Store name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Store name must be between 2 and 200 characters.")]
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store description.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Store description is required.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Store description must be between 10 and 2000 characters.")]
    public string StoreDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store logo URL.
    /// </summary>
    [BindProperty]
    [StringLength(500, ErrorMessage = "Store logo URL must be at most 500 characters.")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string? StoreLogoUrl { get; set; }

    /// <summary>
    /// Gets the current onboarding record.
    /// </summary>
    public SellerOnboarding? Onboarding { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the current step number (1-based).
    /// </summary>
    public int CurrentStepNumber => 1;

    /// <summary>
    /// Gets the total number of steps.
    /// </summary>
    public int TotalSteps => 3;

    public async Task<IActionResult> OnGetAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            return RedirectToPage("/Seller/Login");
        }

        try
        {
            Onboarding = await _onboardingService.GetOrCreateOnboardingAsync(sellerId);

            // If onboarding is complete, redirect to dashboard
            if (Onboarding.Status == OnboardingStatus.PendingVerification ||
                Onboarding.Status == OnboardingStatus.Verified)
            {
                return RedirectToPage("/Seller/Index");
            }

            // Pre-fill form with existing data
            StoreName = Onboarding.StoreName ?? string.Empty;
            StoreDescription = Onboarding.StoreDescription ?? string.Empty;
            StoreLogoUrl = Onboarding.StoreLogoUrl;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading onboarding for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your onboarding data. Please try again.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            return RedirectToPage("/Seller/Login");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var command = new SaveStoreProfileCommand
            {
                SellerId = sellerId,
                StoreName = StoreName,
                StoreDescription = StoreDescription,
                StoreLogoUrl = StoreLogoUrl
            };

            var result = await _onboardingService.SaveStoreProfileAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Store profile saved for seller {SellerId}", sellerId);
                return RedirectToPage("VerificationData");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving store profile for seller {SellerId}", sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while saving your data. Please try again.");
            return Page();
        }
    }
}
