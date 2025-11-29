using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Settlements;

/// <summary>
/// Page model for the settlement details page.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="settlementService">The settlement service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(ISettlementService settlementService, ILogger<DetailsModel> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the settlement.
    /// </summary>
    public Settlement? Settlement { get; set; }

    /// <summary>
    /// Handles GET requests to view settlement details.
    /// </summary>
    /// <param name="id">The settlement ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _settlementService.GetSettlementAsync(id);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to get settlement {SettlementId}: {Errors}", id, string.Join(", ", result.Errors));
            TempData["Error"] = string.Join(", ", result.Errors);
            return Page();
        }

        Settlement = result.Settlement;
        return Page();
    }

    /// <summary>
    /// Handles POST requests to finalize a settlement.
    /// </summary>
    /// <param name="id">The settlement ID.</param>
    public async Task<IActionResult> OnPostFinalizeAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _settlementService.FinalizeSettlementAsync(id);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to finalize settlement {SettlementId}: {Errors}", id, string.Join(", ", result.Errors));
            TempData["Error"] = string.Join(", ", result.Errors);
            return RedirectToPage("Details", new { id });
        }

        TempData["Success"] = "Settlement finalized successfully.";
        return RedirectToPage("Details", new { id });
    }
}
