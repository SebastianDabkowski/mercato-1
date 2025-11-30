using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Currencies;

/// <summary>
/// Page model for setting a currency as the base currency.
/// </summary>
[Authorize(Roles = "Admin")]
public class SetBaseCurrencyModel : PageModel
{
    private readonly ICurrencyManagementService _currencyService;
    private readonly ILogger<SetBaseCurrencyModel> _logger;

    private const string ConfirmationCode = "CONFIRM_BASE_CURRENCY_CHANGE";

    /// <summary>
    /// Initializes a new instance of the <see cref="SetBaseCurrencyModel"/> class.
    /// </summary>
    /// <param name="currencyService">The currency management service.</param>
    /// <param name="logger">The logger.</param>
    public SetBaseCurrencyModel(
        ICurrencyManagementService currencyService,
        ILogger<SetBaseCurrencyModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the currency to set as base.
    /// </summary>
    public Currency? Currency { get; set; }

    /// <summary>
    /// Gets or sets the current base currency.
    /// </summary>
    public Currency? CurrentBaseCurrency { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets whether the user has confirmed the operation.
    /// </summary>
    [BindProperty]
    public bool Confirmed { get; set; }

    /// <summary>
    /// Handles GET requests to the set base currency page.
    /// </summary>
    /// <param name="id">The currency ID to set as base.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _currencyService.GetCurrencyByIdAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        if (result.Currency == null)
        {
            return NotFound();
        }

        if (result.Currency.IsBaseCurrency)
        {
            TempData["ErrorMessage"] = "This currency is already the base currency.";
            return RedirectToPage("Index");
        }

        Currency = result.Currency;

        // Get the current base currency
        var allResult = await _currencyService.GetAllCurrenciesAsync();
        if (allResult.Succeeded)
        {
            CurrentBaseCurrency = allResult.Currencies.FirstOrDefault(c => c.IsBaseCurrency);
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to set the currency as base.
    /// </summary>
    /// <param name="id">The currency ID to set as base.</param>
    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        if (!Confirmed)
        {
            ErrorMessage = "You must confirm that you understand the impact of this operation.";
            return await OnGetAsync(id);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var result = await _currencyService.SetBaseCurrencyAsync(id, userId, userEmail, ConfirmationCode);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            if (result.RequiresConfirmation)
            {
                // This shouldn't happen since we're passing the confirmation code
                ErrorMessage = result.WarningMessage;
                return await OnGetAsync(id);
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        _logger.LogInformation(
            "Currency '{Code}' set as base currency by user {UserId}. Previous base: {PreviousBase}",
            result.Currency?.Code,
            userId,
            result.PreviousBaseCurrency?.Code ?? "None");

        TempData["SuccessMessage"] = $"Currency '{result.Currency?.Code}' has been set as the base currency.";
        return RedirectToPage("Index");
    }
}
