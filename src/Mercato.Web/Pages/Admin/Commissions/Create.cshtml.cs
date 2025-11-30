using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Mercato.Web.Pages.Admin.Commissions;

/// <summary>
/// Page model for creating a new commission rule.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ICommissionRuleManagementService _commissionRuleService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="commissionRuleService">The commission rule management service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        ICommissionRuleManagementService commissionRuleService,
        ILogger<CreateModel> logger)
    {
        _commissionRuleService = commissionRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for creating a rule.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Input model for creating a commission rule.
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
        /// Gets or sets the seller ID (optional, leave empty for global/category rules).
        /// </summary>
        [Display(Name = "Seller ID (optional)")]
        public string? SellerId { get; set; }

        /// <summary>
        /// Gets or sets the category ID (optional, leave empty for global/seller rules).
        /// </summary>
        [Display(Name = "Category ID (optional)")]
        [StringLength(100, ErrorMessage = "Category ID must not exceed 100 characters.")]
        public string? CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the commission rate as a percentage.
        /// </summary>
        [Required(ErrorMessage = "Commission rate is required.")]
        [Range(0, 100, ErrorMessage = "Commission rate must be between 0 and 100.")]
        [Display(Name = "Commission Rate (%)")]
        public decimal CommissionRate { get; set; } = 10.0m;

        /// <summary>
        /// Gets or sets the fixed fee amount.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Fixed fee cannot be negative.")]
        [Display(Name = "Fixed Fee")]
        public decimal FixedFee { get; set; }

        /// <summary>
        /// Gets or sets the optional minimum commission.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Minimum commission cannot be negative.")]
        [Display(Name = "Minimum Commission (optional)")]
        public decimal? MinCommission { get; set; }

        /// <summary>
        /// Gets or sets the optional maximum commission.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Maximum commission cannot be negative.")]
        [Display(Name = "Maximum Commission (optional)")]
        public decimal? MaxCommission { get; set; }

        /// <summary>
        /// Gets or sets the priority of this rule.
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Priority must be between 0 and 1000.")]
        [Display(Name = "Priority")]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets the effective date.
        /// </summary>
        [Required(ErrorMessage = "Effective date is required.")]
        [Display(Name = "Effective Date")]
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;

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
    /// Handles POST requests to create a new commission rule.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Guid? sellerId = null;
        if (!string.IsNullOrWhiteSpace(Input.SellerId))
        {
            if (Guid.TryParse(Input.SellerId, out var parsedSellerId))
            {
                sellerId = parsedSellerId;
            }
            else
            {
                ModelState.AddModelError("Input.SellerId", "Invalid Seller ID format.");
                return Page();
            }
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var command = new CreateCommissionRuleCommand
        {
            Name = Input.Name,
            SellerId = sellerId,
            CategoryId = string.IsNullOrWhiteSpace(Input.CategoryId) ? null : Input.CategoryId.Trim(),
            CommissionRate = Input.CommissionRate,
            FixedFee = Input.FixedFee,
            MinCommission = Input.MinCommission,
            MaxCommission = Input.MaxCommission,
            Priority = Input.Priority,
            EffectiveDate = new DateTimeOffset(Input.EffectiveDate, TimeSpan.Zero),
            IsActive = Input.IsActive,
            CreatedByUserId = userId
        };

        var result = await _commissionRuleService.CreateRuleAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            if (result.ConflictingRules.Count > 0)
            {
                ModelState.AddModelError(string.Empty, "Conflicting commission rules found:");
                foreach (var conflict in result.ConflictingRules)
                {
                    ModelState.AddModelError(string.Empty, $"- {conflict.Name}: {conflict.GetDescription()}");
                }
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            return Page();
        }

        _logger.LogInformation(
            "Commission rule '{Name}' created by user {UserId}",
            command.Name,
            userId);

        TempData["SuccessMessage"] = $"Commission rule '{command.Name}' was created successfully.";
        return RedirectToPage("Index");
    }
}
