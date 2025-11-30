using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Currencies;

/// <summary>
/// Page model for the currencies index page.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ICurrencyManagementService _currencyService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="currencyService">The currency management service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        ICurrencyManagementService currencyService,
        ILogger<IndexModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of currencies.
    /// </summary>
    public IReadOnlyList<Currency> Currencies { get; set; } = [];

    /// <summary>
    /// Gets or sets the base currency.
    /// </summary>
    public Currency? BaseCurrency { get; set; }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the currencies index page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var result = await _currencyService.GetAllCurrenciesAsync();

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogWarning("Failed to get currencies: {Errors}", string.Join(", ", result.Errors));
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Currencies = result.Currencies;
        BaseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
        return Page();
    }

    /// <summary>
    /// Handles POST requests to enable a currency.
    /// </summary>
    /// <param name="id">The currency ID to enable.</param>
    public async Task<IActionResult> OnPostEnableAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var result = await _currencyService.EnableCurrencyAsync(id, userId, userEmail);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Currency '{result.Currency?.Code}' has been enabled.";
        return RedirectToPage();
    }

    /// <summary>
    /// Handles POST requests to disable a currency.
    /// </summary>
    /// <param name="id">The currency ID to disable.</param>
    public async Task<IActionResult> OnPostDisableAsync(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var result = await _currencyService.DisableCurrencyAsync(id, userId, userEmail);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage();
        }

        TempData["SuccessMessage"] = $"Currency '{result.Currency?.Code}' has been disabled.";
        return RedirectToPage();
    }
}
