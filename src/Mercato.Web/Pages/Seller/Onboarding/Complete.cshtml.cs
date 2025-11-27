using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Onboarding;

/// <summary>
/// Page model for the completion step of the seller onboarding wizard.
/// </summary>
[Authorize(Roles = "Buyer,Seller")]
public class CompleteModel : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;
    private readonly ILogger<CompleteModel> _logger;

    public CompleteModel(
        ISellerOnboardingService onboardingService,
        ILogger<CompleteModel> logger)
    {
        _onboardingService = onboardingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current onboarding record.
    /// </summary>
    public SellerOnboarding? Onboarding { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets or sets whether the onboarding was just completed.
    /// </summary>
    public bool JustCompleted { get; private set; }

    /// <summary>
    /// Gets validation errors for store profile step.
    /// </summary>
    public IReadOnlyList<string> StoreProfileErrors { get; private set; } = [];

    /// <summary>
    /// Gets validation errors for verification data step.
    /// </summary>
    public IReadOnlyList<string> VerificationDataErrors { get; private set; } = [];

    /// <summary>
    /// Gets validation errors for payout basics step.
    /// </summary>
    public IReadOnlyList<string> PayoutBasicsErrors { get; private set; } = [];

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
            Onboarding = await _onboardingService.GetOnboardingAsync(sellerId);

            // If no onboarding exists, start from beginning
            if (Onboarding == null)
            {
                return RedirectToPage("StoreProfile");
            }

            // If already completed, show success message
            if (Onboarding.Status == OnboardingStatus.PendingVerification ||
                Onboarding.Status == OnboardingStatus.Verified)
            {
                JustCompleted = false;
                return Page();
            }

            // Validate all steps are complete
            StoreProfileErrors = _onboardingService.GetStoreProfileValidationErrors(Onboarding);
            VerificationDataErrors = _onboardingService.GetVerificationDataValidationErrors(Onboarding);
            PayoutBasicsErrors = _onboardingService.GetPayoutBasicsValidationErrors(Onboarding);

            // Redirect to incomplete steps
            if (StoreProfileErrors.Count > 0)
            {
                return RedirectToPage("StoreProfile");
            }
            if (VerificationDataErrors.Count > 0)
            {
                return RedirectToPage("VerificationData");
            }
            if (PayoutBasicsErrors.Count > 0)
            {
                return RedirectToPage("PayoutBasics");
            }

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

        try
        {
            Onboarding = await _onboardingService.GetOnboardingAsync(sellerId);
            if (Onboarding == null)
            {
                return RedirectToPage("StoreProfile");
            }

            // If already completed, redirect to dashboard
            if (Onboarding.Status == OnboardingStatus.PendingVerification ||
                Onboarding.Status == OnboardingStatus.Verified)
            {
                return RedirectToPage("/Seller/Index");
            }

            var result = await _onboardingService.CompleteOnboardingAsync(sellerId);

            if (result.Succeeded)
            {
                _logger.LogInformation("Onboarding completed for seller {SellerId}", sellerId);
                JustCompleted = true;
                Onboarding = await _onboardingService.GetOnboardingAsync(sellerId);
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            // Refresh validation errors for display
            StoreProfileErrors = _onboardingService.GetStoreProfileValidationErrors(Onboarding);
            VerificationDataErrors = _onboardingService.GetVerificationDataValidationErrors(Onboarding);
            PayoutBasicsErrors = _onboardingService.GetPayoutBasicsValidationErrors(Onboarding);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding for seller {SellerId}", sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while completing your onboarding. Please try again.");
            return Page();
        }
    }
}
