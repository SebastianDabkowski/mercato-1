using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Currencies;

/// <summary>
/// Page model for editing an existing currency.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ICurrencyManagementService _currencyService;
    private readonly ILogger<EditModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="currencyService">The currency management service.</param>
    /// <param name="logger">The logger.</param>
    public EditModel(
        ICurrencyManagementService currencyService,
        ILogger<EditModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for editing a currency.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the currency code (read-only).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the base currency.
    /// </summary>
    public bool IsBaseCurrency { get; set; }

    /// <summary>
    /// Gets or sets whether this currency is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the exchange rate to base currency.
    /// </summary>
    public decimal? ExchangeRateToBase { get; set; }

    /// <summary>
    /// Gets or sets the exchange rate source.
    /// </summary>
    public string? ExchangeRateSource { get; set; }

    /// <summary>
    /// Gets or sets when the exchange rate was last updated.
    /// </summary>
    public DateTimeOffset? ExchangeRateUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the created date for display.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the created by user ID for display.
    /// </summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the updated date for display.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated by user ID for display.
    /// </summary>
    public string? UpdatedByUserId { get; set; }

    /// <summary>
    /// Input model for editing a currency.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the currency ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the currency name.
        /// </summary>
        [Required(ErrorMessage = "Currency name is required.")]
        [StringLength(100, ErrorMessage = "Currency name must not exceed 100 characters.")]
        [Display(Name = "Currency Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the currency symbol.
        /// </summary>
        [Required(ErrorMessage = "Currency symbol is required.")]
        [StringLength(5, ErrorMessage = "Currency symbol must not exceed 5 characters.")]
        [Display(Name = "Symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of decimal places.
        /// </summary>
        [Range(0, 8, ErrorMessage = "Decimal places must be between 0 and 8.")]
        [Display(Name = "Decimal Places")]
        public int DecimalPlaces { get; set; } = 2;
    }

    /// <summary>
    /// Handles GET requests to the edit page.
    /// </summary>
    /// <param name="id">The currency ID to edit.</param>
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

        var currency = result.Currency;
        Input = new InputModel
        {
            Id = currency.Id,
            Name = currency.Name,
            Symbol = currency.Symbol,
            DecimalPlaces = currency.DecimalPlaces
        };

        Code = currency.Code;
        IsBaseCurrency = currency.IsBaseCurrency;
        IsEnabled = currency.IsEnabled;
        ExchangeRateToBase = currency.ExchangeRateToBase;
        ExchangeRateSource = currency.ExchangeRateSource;
        ExchangeRateUpdatedAt = currency.ExchangeRateUpdatedAt;
        CreatedAt = currency.CreatedAt;
        CreatedByUserId = currency.CreatedByUserId;
        UpdatedAt = currency.UpdatedAt;
        UpdatedByUserId = currency.UpdatedByUserId;

        return Page();
    }

    /// <summary>
    /// Handles POST requests to update the currency.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new UpdateCurrencyCommand
        {
            Id = Input.Id,
            Name = Input.Name,
            Symbol = Input.Symbol,
            DecimalPlaces = Input.DecimalPlaces,
            UpdatedByUserId = userId,
            UpdatedByUserEmail = userEmail
        };

        var result = await _currencyService.UpdateCurrencyAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }

        _logger.LogInformation(
            "Currency '{Code}' (ID: {Id}) updated by user {UserId}",
            result.Currency?.Code,
            Input.Id,
            userId);

        TempData["SuccessMessage"] = $"Currency '{result.Currency?.Code}' was updated successfully.";
        return RedirectToPage("Index");
    }
}
