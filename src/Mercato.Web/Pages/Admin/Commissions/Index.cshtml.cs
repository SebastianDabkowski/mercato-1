using Mercato.Admin.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Commissions;

/// <summary>
/// Page model for the commission rules index page.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ICommissionRuleManagementService _commissionRuleService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="commissionRuleService">The commission rule management service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        ICommissionRuleManagementService commissionRuleService,
        ILogger<IndexModel> logger)
    {
        _commissionRuleService = commissionRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of commission rules.
    /// </summary>
    public IReadOnlyList<CommissionRule> Rules { get; set; } = [];

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
    /// Handles GET requests to the commission rules index page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var result = await _commissionRuleService.GetAllRulesAsync();

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogWarning("Failed to get commission rules: {Errors}", string.Join(", ", result.Errors));
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Rules = result.Rules;
        return Page();
    }
}
