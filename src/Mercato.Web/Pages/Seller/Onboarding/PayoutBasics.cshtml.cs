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
/// Page model for the payout basics step of the seller onboarding wizard.
/// </summary>
[Authorize(Roles = "Buyer,Seller")]
public class PayoutBasicsModel : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;
    private readonly ILogger<PayoutBasicsModel> _logger;

    public PayoutBasicsModel(
        ISellerOnboardingService onboardingService,
        ILogger<PayoutBasicsModel> logger)
    {
        _onboardingService = onboardingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the bank name.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Bank name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Bank name must be between 2 and 200 characters.")]
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bank account number.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Bank account number is required.")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Bank account number must be between 5 and 50 characters.")]
    public string BankAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bank routing number.
    /// </summary>
    [BindProperty]
    [StringLength(20, ErrorMessage = "Bank routing number must be at most 20 characters.")]
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Gets or sets the account holder name.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Account holder name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Account holder name must be between 2 and 200 characters.")]
    public string AccountHolderName { get; set; } = string.Empty;

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
    public int CurrentStepNumber => 3;

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

            // If no onboarding exists or previous steps not complete, redirect back
            if (Onboarding == null)
            {
                return RedirectToPage("StoreProfile");
            }

            if (!Onboarding.IsStoreProfileComplete)
            {
                return RedirectToPage("StoreProfile");
            }

            if (!Onboarding.IsVerificationDataComplete)
            {
                return RedirectToPage("VerificationData");
            }

            // If onboarding is complete, redirect to dashboard
            if (Onboarding.Status == OnboardingStatus.PendingVerification ||
                Onboarding.Status == OnboardingStatus.Verified)
            {
                return RedirectToPage("/Seller/Index");
            }

            // Pre-fill form with existing data
            BankName = Onboarding.BankName ?? string.Empty;
            BankAccountNumber = Onboarding.BankAccountNumber ?? string.Empty;
            BankRoutingNumber = Onboarding.BankRoutingNumber;
            AccountHolderName = Onboarding.AccountHolderName ?? string.Empty;

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
            var command = new SavePayoutBasicsCommand
            {
                SellerId = sellerId,
                BankName = BankName,
                BankAccountNumber = BankAccountNumber,
                BankRoutingNumber = BankRoutingNumber,
                AccountHolderName = AccountHolderName
            };

            var result = await _onboardingService.SavePayoutBasicsAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Payout basics saved for seller {SellerId}", sellerId);
                return RedirectToPage("Complete");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payout basics for seller {SellerId}", sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while saving your data. Please try again.");
            return Page();
        }
    }
}
