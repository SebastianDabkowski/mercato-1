using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin;

/// <summary>
/// Page model for the marketplace performance dashboard displaying KPIs.
/// </summary>
[Authorize(Roles = "Admin")]
public class PerformanceDashboardModel : PageModel
{
    private readonly IMarketplaceDashboardService _dashboardService;
    private readonly ILogger<PerformanceDashboardModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceDashboardModel"/> class.
    /// </summary>
    /// <param name="dashboardService">The marketplace dashboard service.</param>
    /// <param name="logger">The logger.</param>
    public PerformanceDashboardModel(
        IMarketplaceDashboardService dashboardService,
        ILogger<PerformanceDashboardModel> logger)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);
        ArgumentNullException.ThrowIfNull(logger);

        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the dashboard result containing marketplace KPIs.
    /// </summary>
    public MarketplaceDashboardResult? DashboardResult { get; set; }

    /// <summary>
    /// Gets or sets the selected time period for filtering.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string TimePeriod { get; set; } = "30d";

    /// <summary>
    /// Gets a value indicating whether there is no data for the selected period.
    /// </summary>
    public bool HasNoData => DashboardResult != null &&
        DashboardResult.TotalGmv == 0 &&
        DashboardResult.TotalOrders == 0 &&
        DashboardResult.ActiveSellers == 0 &&
        DashboardResult.ActiveProducts == 0 &&
        DashboardResult.NewUsers == 0;

    /// <summary>
    /// Handles GET requests to load the performance dashboard.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing performance dashboard with period: {TimePeriod}", TimePeriod);

        var (startDate, endDate) = GetDateRange(TimePeriod);

        var query = new MarketplaceDashboardQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        DashboardResult = await _dashboardService.GetDashboardAsync(query);
    }

    /// <summary>
    /// Formats a decimal value as currency.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted currency string.</returns>
    public static string FormatCurrency(decimal value)
    {
        return value.ToString("C0");
    }

    /// <summary>
    /// Gets the display text for a time period.
    /// </summary>
    /// <param name="timePeriod">The time period code.</param>
    /// <returns>The display text.</returns>
    public static string GetTimePeriodDisplay(string timePeriod) => timePeriod switch
    {
        "today" => "Today",
        "7d" => "Last 7 Days",
        "30d" => "Last 30 Days",
        _ => "Last 30 Days"
    };

    private static (DateTimeOffset StartDate, DateTimeOffset EndDate) GetDateRange(string timePeriod)
    {
        var endDate = DateTimeOffset.UtcNow;
        var startDate = timePeriod switch
        {
            "today" => new DateTimeOffset(endDate.Date, TimeSpan.Zero),
            "7d" => endDate.AddDays(-7),
            "30d" => endDate.AddDays(-30),
            _ => endDate.AddDays(-30)
        };

        return (startDate, endDate);
    }
}
