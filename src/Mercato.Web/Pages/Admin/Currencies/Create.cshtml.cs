using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Currencies;

/// <summary>
/// Page model for creating a new currency.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ICurrencyManagementService _currencyService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="currencyService">The currency management service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        ICurrencyManagementService currencyService,
        ILogger<CreateModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for creating a currency.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Input model for creating a currency.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the ISO 4217 currency code.
        /// </summary>
        [Required(ErrorMessage = "Currency code is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters.")]
        [Display(Name = "Currency Code")]
        public string Code { get; set; } = string.Empty;

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

        /// <summary>
        /// Gets or sets whether this currency is enabled.
        /// </summary>
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Handles GET requests to the create page.
    /// </summary>
    public void OnGet()
    {
        // Initialize with default values
    }

    /// <summary>
    /// Handles POST requests to create a new currency.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new CreateCurrencyCommand
        {
            Code = Input.Code.ToUpperInvariant(),
            Name = Input.Name,
            Symbol = Input.Symbol,
            DecimalPlaces = Input.DecimalPlaces,
            IsEnabled = Input.IsEnabled,
            CreatedByUserId = userId,
            CreatedByUserEmail = userEmail
        };

        var result = await _currencyService.CreateCurrencyAsync(command);

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
            "Currency '{Code}' ({Name}) created by user {UserId}",
            command.Code,
            command.Name,
            userId);

        TempData["SuccessMessage"] = $"Currency '{command.Code}' ({command.Name}) was created successfully.";
        return RedirectToPage("Index");
    }
}
