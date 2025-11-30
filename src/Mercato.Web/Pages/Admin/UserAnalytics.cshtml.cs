using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin;

/// <summary>
/// Page model for the user analytics dashboard displaying registration and activity metrics.
/// All metrics are aggregated and anonymized to comply with privacy requirements.
/// </summary>
[Authorize(Roles = "Admin")]
public class UserAnalyticsModel : PageModel
{
    private readonly IUserAnalyticsService _analyticsService;
    private readonly ILogger<UserAnalyticsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAnalyticsModel"/> class.
    /// </summary>
    /// <param name="analyticsService">The user analytics service.</param>
    /// <param name="logger">The logger.</param>
    public UserAnalyticsModel(
        IUserAnalyticsService analyticsService,
        ILogger<UserAnalyticsModel> logger)
    {
        ArgumentNullException.ThrowIfNull(analyticsService);
        ArgumentNullException.ThrowIfNull(logger);

        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the analytics result containing user metrics.
    /// </summary>
    public UserAnalyticsResult? AnalyticsResult { get; set; }

    /// <summary>
    /// Gets or sets the selected time period for filtering.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string TimePeriod { get; set; } = "30d";

    /// <summary>
    /// Handles GET requests to load the user analytics dashboard.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing user analytics with period: {TimePeriod}", TimePeriod);

        var (startDate, endDate) = GetDateRange(TimePeriod);

        var query = new UserAnalyticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };

        AnalyticsResult = await _analyticsService.GetAnalyticsAsync(query);
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
        "90d" => "Last 90 Days",
        _ => "Last 30 Days"
    };

    /// <summary>
    /// Gets the CSS class for a metric card based on data availability.
    /// </summary>
    /// <param name="hasData">Whether data is available.</param>
    /// <returns>The CSS class.</returns>
    public static string GetCardClass(bool hasData) =>
        hasData ? "" : "opacity-50";

    private static (DateTimeOffset StartDate, DateTimeOffset EndDate) GetDateRange(string timePeriod)
    {
        var endDate = DateTimeOffset.UtcNow;
        var startDate = timePeriod switch
        {
            "today" => new DateTimeOffset(endDate.Date, TimeSpan.Zero),
            "7d" => endDate.AddDays(-7),
            "30d" => endDate.AddDays(-30),
            "90d" => endDate.AddDays(-90),
            _ => endDate.AddDays(-30)
        };

        return (startDate, endDate);
    }
}
