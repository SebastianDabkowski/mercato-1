using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    /// Gets or sets the seller type.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Seller type is required.")]
    public SellerType SellerType { get; set; } = SellerType.Individual;

    /// <summary>
    /// Gets the seller type options for dropdown.
    /// </summary>
    public List<SelectListItem> SellerTypeOptions { get; } = new()
    {
        new SelectListItem { Value = "0", Text = "Individual" },
        new SelectListItem { Value = "1", Text = "Company" }
    };

    // Company-specific fields

    /// <summary>
    /// Gets or sets the business name (for Company sellers).
    /// </summary>
    [BindProperty]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Business name must be between 2 and 200 characters.")]
    public string? BusinessName { get; set; }

    /// <summary>
    /// Gets or sets the business registration number (for Company sellers).
    /// </summary>
    [BindProperty]
    [StringLength(50, ErrorMessage = "Business registration number must be at most 50 characters.")]
    public string? BusinessRegistrationNumber { get; set; }

    /// <summary>
    /// Gets or sets the contact person name (for Company sellers).
    /// </summary>
    [BindProperty]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Contact person name must be between 2 and 200 characters.")]
    public string? ContactPersonName { get; set; }

    /// <summary>
    /// Gets or sets the contact person email (for Company sellers).
    /// </summary>
    [BindProperty]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(254, ErrorMessage = "Contact person email must be at most 254 characters.")]
    public string? ContactPersonEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact person phone (for Company sellers).
    /// </summary>
    [BindProperty]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Contact person phone must be at most 20 characters.")]
    public string? ContactPersonPhone { get; set; }

    // Individual-specific fields

    /// <summary>
    /// Gets or sets the full name (for Individual sellers).
    /// </summary>
    [BindProperty]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the personal ID number (for Individual sellers).
    /// </summary>
    [BindProperty]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Personal ID number must be between 5 and 50 characters.")]
    public string? PersonalIdNumber { get; set; }

    // Common fields

    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 500 characters.")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax identification number.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Tax ID is required.")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Tax ID must be between 5 and 50 characters.")]
    public string TaxId { get; set; } = string.Empty;

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
            SellerType = Onboarding.SellerType;
            Address = Onboarding.BusinessAddress ?? string.Empty;
            TaxId = Onboarding.TaxId ?? string.Empty;

            if (Onboarding.SellerType == SellerType.Company)
            {
                BusinessName = Onboarding.BusinessName;
                BusinessRegistrationNumber = Onboarding.BusinessRegistrationNumber;
                ContactPersonName = Onboarding.ContactPersonName;
                ContactPersonEmail = Onboarding.ContactPersonEmail;
                ContactPersonPhone = Onboarding.ContactPersonPhone;
            }
            else
            {
                FullName = Onboarding.FullName;
                PersonalIdNumber = Onboarding.PersonalIdNumber;
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

        // Clear validation errors for fields not applicable to the selected seller type
        ClearInapplicableValidationErrors();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var command = new SaveVerificationDataCommand
            {
                SellerId = sellerId,
                SellerType = SellerType,
                BusinessAddress = Address,
                TaxId = TaxId
            };

            if (SellerType == SellerType.Company)
            {
                command.BusinessName = BusinessName;
                command.BusinessRegistrationNumber = BusinessRegistrationNumber;
                command.ContactPersonName = ContactPersonName;
                command.ContactPersonEmail = ContactPersonEmail;
                command.ContactPersonPhone = ContactPersonPhone;
            }
            else
            {
                command.FullName = FullName;
                command.PersonalIdNumber = PersonalIdNumber;
            }

            var result = await _onboardingService.SaveVerificationDataAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Verification data saved for seller {SellerId} as {SellerType}", sellerId, SellerType);
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

    /// <summary>
    /// Clears validation errors for fields that are not applicable to the selected seller type.
    /// </summary>
    private void ClearInapplicableValidationErrors()
    {
        if (SellerType == SellerType.Company)
        {
            // Clear individual field errors
            ModelState.Remove(nameof(FullName));
            ModelState.Remove(nameof(PersonalIdNumber));
        }
        else
        {
            // Clear company field errors
            ModelState.Remove(nameof(BusinessName));
            ModelState.Remove(nameof(BusinessRegistrationNumber));
            ModelState.Remove(nameof(ContactPersonName));
            ModelState.Remove(nameof(ContactPersonEmail));
            ModelState.Remove(nameof(ContactPersonPhone));
        }
    }
}
