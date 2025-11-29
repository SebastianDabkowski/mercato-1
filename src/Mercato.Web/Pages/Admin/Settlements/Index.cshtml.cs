using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mercato.Web.Pages.Admin.Settlements;

/// <summary>
/// Page model for the settlements index page.
/// </summary>
public class IndexModel : PageModel
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="settlementService">The settlement service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(ISettlementService settlementService, ILogger<IndexModel> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of settlements.
    /// </summary>
    public IReadOnlyList<Settlement> Settlements { get; set; } = [];

    /// <summary>
    /// Gets or sets the year filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the month filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public SettlementStatus? Status { get; set; }

    /// <summary>
    /// Gets the year options for the filter dropdown.
    /// </summary>
    public List<SelectListItem> YearOptions { get; } = [];

    /// <summary>
    /// Gets the month options for the filter dropdown.
    /// </summary>
    public List<SelectListItem> MonthOptions { get; } = [];

    /// <summary>
    /// Gets the status options for the filter dropdown.
    /// </summary>
    public List<SelectListItem> StatusOptions { get; } = [];

    /// <summary>
    /// Handles GET requests to the settlements index page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        PopulateFilterOptions();

        var query = new GetSettlementsQuery
        {
            Year = Year,
            Month = Month,
            Status = Status
        };

        var result = await _settlementService.GetSettlementsAsync(query);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to get settlements: {Errors}", string.Join(", ", result.Errors));
            TempData["Error"] = string.Join(", ", result.Errors);
            return Page();
        }

        Settlements = result.Settlements;
        return Page();
    }

    private void PopulateFilterOptions()
    {
        // Populate year options (last 5 years)
        var currentYear = DateTime.UtcNow.Year;
        for (var year = currentYear; year >= currentYear - 5; year--)
        {
            YearOptions.Add(new SelectListItem(year.ToString(), year.ToString(), year == Year));
        }

        // Populate month options
        for (var month = 1; month <= 12; month++)
        {
            var monthName = new DateTime(2000, month, 1).ToString("MMMM");
            MonthOptions.Add(new SelectListItem(monthName, month.ToString(), month == Month));
        }

        // Populate status options
        foreach (SettlementStatus status in Enum.GetValues(typeof(SettlementStatus)))
        {
            StatusOptions.Add(new SelectListItem(status.ToString(), ((int)status).ToString(), status == Status));
        }
    }
}
