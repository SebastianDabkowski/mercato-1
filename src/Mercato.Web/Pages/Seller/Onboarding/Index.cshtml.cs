using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Onboarding;

/// <summary>
/// Page model for the seller onboarding wizard index page.
/// Redirects users to their current step in the onboarding process.
/// </summary>
[Authorize(Roles = "Buyer,Seller")]
public class IndexModel : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ISellerOnboardingService onboardingService,
        ILogger<IndexModel> logger)
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

            // Redirect to the current step
            return Onboarding.CurrentStep switch
            {
                OnboardingStep.StoreProfile => RedirectToPage("StoreProfile"),
                OnboardingStep.VerificationData => RedirectToPage("VerificationData"),
                OnboardingStep.PayoutBasics => RedirectToPage("PayoutBasics"),
                OnboardingStep.Completed => RedirectToPage("Complete"),
                _ => RedirectToPage("StoreProfile")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading onboarding for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your onboarding progress. Please try again.";
            return Page();
        }
    }
}
