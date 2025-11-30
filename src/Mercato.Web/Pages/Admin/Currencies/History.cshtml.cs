using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Currencies;

/// <summary>
/// Page model for viewing currency history.
/// </summary>
[Authorize(Roles = "Admin")]
public class HistoryModel : PageModel
{
    private readonly ICurrencyManagementService _currencyService;
    private readonly ILogger<HistoryModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryModel"/> class.
    /// </summary>
    /// <param name="currencyService">The currency management service.</param>
    /// <param name="logger">The logger.</param>
    public HistoryModel(
        ICurrencyManagementService currencyService,
        ILogger<HistoryModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public Currency? Currency { get; set; }

    /// <summary>
    /// Gets or sets the history records.
    /// </summary>
    public IReadOnlyList<CurrencyHistory> History { get; set; } = [];

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the history page.
    /// </summary>
    /// <param name="id">The currency ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _currencyService.GetCurrencyHistoryAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            _logger.LogWarning("Failed to get currency history: {Errors}", string.Join(", ", result.Errors));
            ErrorMessage = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        Currency = result.Currency;
        History = result.History;

        return Page();
    }
}
