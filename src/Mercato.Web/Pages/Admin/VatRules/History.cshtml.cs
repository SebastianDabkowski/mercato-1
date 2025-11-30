using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.VatRules;

/// <summary>
/// Page model for viewing VAT rule history.
/// </summary>
[Authorize(Roles = "Admin")]
public class HistoryModel : PageModel
{
    private readonly IVatRuleManagementService _vatRuleService;
    private readonly ILogger<HistoryModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryModel"/> class.
    /// </summary>
    /// <param name="vatRuleService">The VAT rule management service.</param>
    /// <param name="logger">The logger.</param>
    public HistoryModel(
        IVatRuleManagementService vatRuleService,
        ILogger<HistoryModel> logger)
    {
        _vatRuleService = vatRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the VAT rule.
    /// </summary>
    public VatRule? Rule { get; set; }

    /// <summary>
    /// Gets or sets the history records.
    /// </summary>
    public IReadOnlyList<VatRuleHistory> History { get; set; } = [];

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the history page.
    /// </summary>
    /// <param name="id">The VAT rule ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _vatRuleService.GetRuleHistoryAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogWarning("Failed to get VAT rule history: {Errors}", string.Join(", ", result.Errors));
            ErrorMessage = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        Rule = result.Rule;
        History = result.History;

        return Page();
    }
}
