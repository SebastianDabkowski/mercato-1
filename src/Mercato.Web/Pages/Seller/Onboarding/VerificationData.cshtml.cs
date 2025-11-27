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
/// Page model for the verification data step of the seller onboarding wizard.
/// </summary>
[Authorize(Roles = "Buyer,Seller")]
public class VerificationDataModel : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;
    private readonly ILogger<VerificationDataModel> _logger;

    public VerificationDataModel(
        ISellerOnboardingService onboardingService,
        ILogger<VerificationDataModel> logger)
    {
        _onboardingService = onboardingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the business name.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Business name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Business name must be between 2 and 200 characters.")]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business address.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Business address is required.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Business address must be between 5 and 500 characters.")]
    public string BusinessAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax identification number.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Tax ID is required.")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Tax ID must be between 5 and 50 characters.")]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business registration number.
    /// </summary>
    [BindProperty]
    [StringLength(50, ErrorMessage = "Business registration number must be at most 50 characters.")]
    public string? BusinessRegistrationNumber { get; set; }

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
    public int CurrentStepNumber => 2;

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
            Onboarding = await _onboardingService.GetOnboardingAsync(sellerId);

            // If no onboarding exists or store profile not complete, redirect back
            if (Onboarding == null || !Onboarding.IsStoreProfileComplete)
            {
                return RedirectToPage("StoreProfile");
            }

            // If onboarding is complete, redirect to dashboard
            if (Onboarding.Status == OnboardingStatus.PendingVerification ||
                Onboarding.Status == OnboardingStatus.Verified)
            {
                return RedirectToPage("/Seller/Index");
            }

            // Pre-fill form with existing data
            BusinessName = Onboarding.BusinessName ?? string.Empty;
            BusinessAddress = Onboarding.BusinessAddress ?? string.Empty;
            TaxId = Onboarding.TaxId ?? string.Empty;
            BusinessRegistrationNumber = Onboarding.BusinessRegistrationNumber;

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
            var command = new SaveVerificationDataCommand
            {
                SellerId = sellerId,
                BusinessName = BusinessName,
                BusinessAddress = BusinessAddress,
                TaxId = TaxId,
                BusinessRegistrationNumber = BusinessRegistrationNumber
            };

            var result = await _onboardingService.SaveVerificationDataAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Verification data saved for seller {SellerId}", sellerId);
                return RedirectToPage("PayoutBasics");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving verification data for seller {SellerId}", sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while saving your data. Please try again.");
            return Page();
        }
    }
}
