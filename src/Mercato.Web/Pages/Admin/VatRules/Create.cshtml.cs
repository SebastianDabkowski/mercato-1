using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.VatRules;

/// <summary>
/// Page model for creating a new VAT rule.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IVatRuleManagementService _vatRuleService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="vatRuleService">The VAT rule management service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        IVatRuleManagementService vatRuleService,
        ILogger<CreateModel> logger)
    {
        _vatRuleService = vatRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for creating a rule.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Input model for creating a VAT rule.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the name of the rule.
        /// </summary>
        [Required(ErrorMessage = "Rule name is required.")]
        [StringLength(200, ErrorMessage = "Rule name must not exceed 200 characters.")]
        [Display(Name = "Rule Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
        [Required(ErrorMessage = "Country code is required.")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters.")]
        [Display(Name = "Country Code")]
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tax rate.
        /// </summary>
        [Required(ErrorMessage = "Tax rate is required.")]
        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
        [Display(Name = "Tax Rate (%)")]
        public decimal TaxRate { get; set; } = 20.0m;

        /// <summary>
        /// Gets or sets the optional category ID.
        /// </summary>
        [Display(Name = "Category ID (optional)")]
        public string? CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the effective from date.
        /// </summary>
        [Required(ErrorMessage = "Effective from date is required.")]
        [Display(Name = "Effective From")]
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;

        /// <summary>
        /// Gets or sets the effective to date.
        /// </summary>
        [Display(Name = "Effective To (optional)")]
        public DateTime? EffectiveTo { get; set; }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Priority must be between 0 and 1000.")]
        [Display(Name = "Priority")]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether this rule is active.
        /// </summary>
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Handles GET requests to the create page.
    /// </summary>
    public void OnGet()
    {
        // Initialize with default values
    }

    /// <summary>
    /// Handles POST requests to create a new VAT rule.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(Input.CategoryId))
        {
            if (Guid.TryParse(Input.CategoryId, out var parsedCategoryId))
            {
                categoryId = parsedCategoryId;
            }
            else
            {
                ModelState.AddModelError("Input.CategoryId", "Invalid Category ID format.");
                return Page();
            }
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new CreateVatRuleCommand
        {
            Name = Input.Name,
            CountryCode = Input.CountryCode.ToUpperInvariant(),
            TaxRate = Input.TaxRate,
            CategoryId = categoryId,
            EffectiveFrom = new DateTimeOffset(Input.EffectiveFrom, TimeSpan.Zero),
            EffectiveTo = Input.EffectiveTo.HasValue 
                ? new DateTimeOffset(Input.EffectiveTo.Value, TimeSpan.Zero) 
                : null,
            Priority = Input.Priority,
            IsActive = Input.IsActive,
            CreatedByUserId = userId,
            CreatedByUserEmail = userEmail
        };

        var result = await _vatRuleService.CreateRuleAsync(command);

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
            "VAT rule '{Name}' created by user {UserId} for country {CountryCode}",
            command.Name,
            userId,
            command.CountryCode);

        TempData["SuccessMessage"] = $"VAT rule '{command.Name}' was created successfully.";
        return RedirectToPage("Index");
    }
}
