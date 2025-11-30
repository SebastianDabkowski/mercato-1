using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Reports;

/// <summary>
/// Page model for the admin commission summary report page with filtering and CSV export.
/// </summary>
[Authorize(Roles = "Admin")]
public class CommissionSummaryModel : PageModel
{
    private readonly ICommissionSummaryService _summaryService;
    private readonly ILogger<CommissionSummaryModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommissionSummaryModel"/> class.
    /// </summary>
    /// <param name="summaryService">The commission summary service.</param>
    /// <param name="logger">The logger.</param>
    public CommissionSummaryModel(
        ICommissionSummaryService summaryService,
        ILogger<CommissionSummaryModel> logger)
    {
        _summaryService = summaryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of summary rows grouped by seller.
    /// </summary>
    public IReadOnlyList<SellerCommissionSummaryRow> Rows { get; private set; } = [];

    /// <summary>
    /// Gets the error message to display.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the total GMV across all sellers.
    /// </summary>
    public decimal TotalGMV { get; private set; }

    /// <summary>
    /// Gets the total commission across all sellers.
    /// </summary>
    public decimal TotalCommission { get; private set; }

    /// <summary>
    /// Gets the total net payout across all sellers.
    /// </summary>
    public decimal TotalNetPayout { get; private set; }

    /// <summary>
    /// Gets the total order count across all sellers.
    /// </summary>
    public int TotalOrderCount { get; private set; }

    /// <summary>
    /// Gets or sets the start date for date range filter (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for date range filter (query parameter).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? ToDate { get; set; }

    /// <summary>
    /// Handles GET requests for the admin commission summary page with filtering.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var query = new CommissionSummaryFilterQuery
        {
            FromDate = FromDate,
            ToDate = ToDate
        };

        var result = await _summaryService.GetSummaryAsync(query);

        if (result.IsNotAuthorized)
        {
            return Forbid();
        }

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Rows = result.Rows;
        TotalGMV = result.TotalGMV;
        TotalCommission = result.TotalCommission;
        TotalNetPayout = result.TotalNetPayout;
        TotalOrderCount = result.TotalOrderCount;

        return Page();
    }

    /// <summary>
    /// Handles the POST request for CSV export.
    /// </summary>
    /// <returns>The file result with the CSV content.</returns>
    public async Task<IActionResult> OnPostExportAsync()
    {
        var query = new CommissionSummaryFilterQuery
        {
            FromDate = FromDate,
            ToDate = ToDate
        };

        var result = await _summaryService.ExportToCsvAsync(query);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            // Reload the page with data
            await OnGetAsync();
            return Page();
        }

        return File(result.CsvContent, "text/csv", result.FileName);
    }

    /// <summary>
    /// Gets the drill-down URL for a specific seller.
    /// </summary>
    /// <param name="sellerId">The seller's unique identifier.</param>
    /// <returns>The drill-down URL.</returns>
    public string GetDetailsUrl(Guid sellerId)
    {
        var queryParams = new List<string> { $"sellerId={sellerId}" };

        if (FromDate.HasValue)
        {
            queryParams.Add($"FromDate={FromDate.Value:yyyy-MM-dd}");
        }

        if (ToDate.HasValue)
        {
            queryParams.Add($"ToDate={ToDate.Value:yyyy-MM-dd}");
        }

        return "./CommissionSummaryDetails?" + string.Join("&", queryParams);
    }
}
