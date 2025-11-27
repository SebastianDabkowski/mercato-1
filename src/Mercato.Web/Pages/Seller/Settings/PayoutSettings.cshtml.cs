using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Settings;

/// <summary>
/// Page model for managing payout settings.
/// </summary>
[Authorize(Roles = "Seller")]
public class PayoutSettingsModel : PageModel
{
    private readonly IPayoutSettingsService _payoutSettingsService;
    private readonly ILogger<PayoutSettingsModel> _logger;

    public PayoutSettingsModel(
        IPayoutSettingsService payoutSettingsService,
        ILogger<PayoutSettingsModel> logger)
    {
        _payoutSettingsService = payoutSettingsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the preferred payout method.
    /// </summary>
    [BindProperty]
    public PayoutMethod PreferredPayoutMethod { get; set; } = PayoutMethod.BankTransfer;

    /// <summary>
    /// Gets or sets the bank name.
    /// </summary>
    [BindProperty]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Bank name must be between 2 and 200 characters.")]
    public string? BankName { get; set; }

    /// <summary>
    /// Gets or sets the bank account number.
    /// </summary>
    [BindProperty]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Bank account number must be between 5 and 50 characters.")]
    public string? BankAccountNumber { get; set; }

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
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Account holder name must be between 2 and 200 characters.")]
    public string? AccountHolderName { get; set; }

    /// <summary>
    /// Gets or sets the SWIFT/BIC code.
    /// </summary>
    [BindProperty]
    [StringLength(11, ErrorMessage = "SWIFT code must be at most 11 characters.")]
    public string? SwiftCode { get; set; }

    /// <summary>
    /// Gets or sets the IBAN.
    /// </summary>
    [BindProperty]
    [StringLength(34, ErrorMessage = "IBAN must be at most 34 characters.")]
    public string? Iban { get; set; }

    /// <summary>
    /// Gets or sets the payment account email.
    /// </summary>
    [BindProperty]
    [StringLength(254, ErrorMessage = "Payment account email must be at most 254 characters.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string? PaymentAccountEmail { get; set; }

    /// <summary>
    /// Gets or sets the payment account ID.
    /// </summary>
    [BindProperty]
    [StringLength(100, ErrorMessage = "Payment account ID must be at most 100 characters.")]
    public string? PaymentAccountId { get; set; }

    /// <summary>
    /// Gets the current payout settings record.
    /// </summary>
    public PayoutSettings? Settings { get; private set; }

    /// <summary>
    /// Gets or sets a success message.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Gets or sets an error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets whether the payout configuration is incomplete.
    /// </summary>
    public bool IsIncomplete => Settings == null || !Settings.IsComplete;

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
            Settings = await _payoutSettingsService.GetPayoutSettingsAsync(sellerId);

            if (Settings != null)
            {
                // Pre-fill form with existing data
                PreferredPayoutMethod = Settings.PreferredPayoutMethod;
                BankName = Settings.BankName;
                BankAccountNumber = Settings.BankAccountNumber;
                BankRoutingNumber = Settings.BankRoutingNumber;
                AccountHolderName = Settings.AccountHolderName;
                SwiftCode = Settings.SwiftCode;
                Iban = Settings.Iban;
                PaymentAccountEmail = Settings.PaymentAccountEmail;
                PaymentAccountId = Settings.PaymentAccountId;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payout settings for seller {SellerId}", sellerId);
            ErrorMessage = "An error occurred while loading your payout settings. Please try again.";
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
            var command = new SavePayoutSettingsCommand
            {
                SellerId = sellerId,
                PreferredPayoutMethod = PreferredPayoutMethod,
                BankName = BankName,
                BankAccountNumber = BankAccountNumber,
                BankRoutingNumber = BankRoutingNumber,
                AccountHolderName = AccountHolderName,
                SwiftCode = SwiftCode,
                Iban = Iban,
                PaymentAccountEmail = PaymentAccountEmail,
                PaymentAccountId = PaymentAccountId
            };

            var result = await _payoutSettingsService.SavePayoutSettingsAsync(command);

            if (result.Succeeded)
            {
                _logger.LogInformation("Payout settings updated for seller {SellerId}", sellerId);
                SuccessMessage = "Payout settings updated successfully.";

                // Reload settings data
                Settings = await _payoutSettingsService.GetPayoutSettingsAsync(sellerId);
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payout settings for seller {SellerId}", sellerId);
            ModelState.AddModelError(string.Empty, "An error occurred while saving your payout settings. Please try again.");
            return Page();
        }
    }
}
