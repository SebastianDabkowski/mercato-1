using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Mercato.Web.Pages.Admin.Commissions;

/// <summary>
/// Page model for editing an existing commission rule.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly ICommissionRuleManagementService _commissionRuleService;
    private readonly ILogger<EditModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="commissionRuleService">The commission rule management service.</param>
    /// <param name="logger">The logger.</param>
    public EditModel(
        ICommissionRuleManagementService commissionRuleService,
        ILogger<EditModel> logger)
    {
        _commissionRuleService = commissionRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for editing a rule.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the rule version for display.
    /// </summary>
    public int CurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the created date for display.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the created by user ID for display.
    /// </summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Input model for editing a commission rule.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the rule ID.
        /// </summary>
        public Guid Id { get; set; }

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
        public decimal CommissionRate { get; set; }

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
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the effective date.
        /// </summary>
        [Required(ErrorMessage = "Effective date is required.")]
        [Display(Name = "Effective Date")]
        public DateTime EffectiveDate { get; set; }

        /// <summary>
        /// Gets or sets whether this rule is active.
        /// </summary>
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Handles GET requests to the edit page.
    /// </summary>
    /// <param name="id">The rule ID to edit.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _commissionRuleService.GetRuleByIdAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        if (result.Rule == null)
        {
            return NotFound();
        }

        var rule = result.Rule;
        Input = new InputModel
        {
            Id = rule.Id,
            Name = rule.Name,
            SellerId = rule.SellerId?.ToString(),
            CategoryId = rule.CategoryId,
            CommissionRate = rule.CommissionRate,
            FixedFee = rule.FixedFee,
            MinCommission = rule.MinCommission,
            MaxCommission = rule.MaxCommission,
            Priority = rule.Priority,
            EffectiveDate = rule.EffectiveDate.UtcDateTime.Date,
            IsActive = rule.IsActive
        };

        CurrentVersion = rule.Version;
        CreatedAt = rule.CreatedAt;
        CreatedByUserId = rule.CreatedByUserId;

        return Page();
    }

    /// <summary>
    /// Handles POST requests to update the commission rule.
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

        var command = new UpdateCommissionRuleCommand
        {
            Id = Input.Id,
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
            ModifiedByUserId = userId
        };

        var result = await _commissionRuleService.UpdateRuleAsync(command);

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
            "Commission rule '{Name}' (ID: {Id}) updated by user {UserId}",
            command.Name,
            command.Id,
            userId);

        TempData["SuccessMessage"] = $"Commission rule '{command.Name}' was updated successfully.";
        return RedirectToPage("Index");
    }
}
