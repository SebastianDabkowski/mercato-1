using System.Text;
using Mercato.Payments.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Settlements;

/// <summary>
/// Page model for the settlement export page.
/// </summary>
public class ExportModel : PageModel
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<ExportModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportModel"/> class.
    /// </summary>
    /// <param name="settlementService">The settlement service.</param>
    /// <param name="logger">The logger.</param>
    public ExportModel(ISettlementService settlementService, ILogger<ExportModel> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    /// <summary>
    /// Handles GET requests to export a settlement as CSV.
    /// </summary>
    /// <param name="id">The settlement ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _settlementService.ExportSettlementAsync(id);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to export settlement {SettlementId}: {Errors}", id, string.Join(", ", result.Errors));
            TempData["Error"] = string.Join(", ", result.Errors);
            return RedirectToPage("Details", new { id });
        }

        var bytes = Encoding.UTF8.GetBytes(result.CsvData!);
        return File(bytes, "text/csv", result.FileName);
    }
}
