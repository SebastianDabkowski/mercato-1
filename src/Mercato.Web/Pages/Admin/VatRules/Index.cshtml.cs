using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.VatRules;

/// <summary>
/// Page model for the VAT rules index page.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IVatRuleManagementService _vatRuleService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="vatRuleService">The VAT rule management service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        IVatRuleManagementService vatRuleService,
        ILogger<IndexModel> logger)
    {
        _vatRuleService = vatRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of VAT rules.
    /// </summary>
    public IReadOnlyList<VatRule> Rules { get; set; } = [];

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
    /// Handles GET requests to the VAT rules index page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var result = await _vatRuleService.GetAllRulesAsync();

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogWarning("Failed to get VAT rules: {Errors}", string.Join(", ", result.Errors));
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Rules = result.Rules;
        return Page();
    }
}
