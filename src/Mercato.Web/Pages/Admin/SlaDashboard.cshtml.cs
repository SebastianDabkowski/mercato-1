using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin;

/// <summary>
/// Page model for the SLA dashboard displaying SLA metrics and breached cases.
/// </summary>
[Authorize(Roles = "Admin")]
public class SlaDashboardModel : PageModel
{
    private readonly ISlaTrackingService _slaTrackingService;
    private readonly ILogger<SlaDashboardModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlaDashboardModel"/> class.
    /// </summary>
    /// <param name="slaTrackingService">The SLA tracking service.</param>
    /// <param name="logger">The logger.</param>
    public SlaDashboardModel(
        ISlaTrackingService slaTrackingService,
        ILogger<SlaDashboardModel> logger)
    {
        _slaTrackingService = slaTrackingService ?? throw new ArgumentNullException(nameof(slaTrackingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or sets the dashboard statistics.
    /// </summary>
    public SlaDashboardStatistics? Statistics { get; set; }

    /// <summary>
    /// Gets or sets the list of breached cases.
    /// </summary>
    public IReadOnlyList<SlaTrackingRecord> BreachedCases { get; set; } = [];

    /// <summary>
    /// Gets or sets the seller statistics.
    /// </summary>
    public IReadOnlyList<SlaStoreStatistics> SellerStatistics { get; set; } = [];

    /// <summary>
    /// Gets or sets the SLA configurations.
    /// </summary>
    public IReadOnlyList<SlaConfiguration> SlaConfigurations { get; set; } = [];

    /// <summary>
    /// Gets or sets the selected time period for filtering.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string TimePeriod { get; set; } = "30d";

    /// <summary>
    /// Handles GET requests to load the SLA dashboard.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing SLA dashboard with period: {TimePeriod}", TimePeriod);

        var (startDate, endDate) = GetDateRange(TimePeriod);

        // Load dashboard statistics
        Statistics = await _slaTrackingService.GetDashboardStatisticsAsync(startDate, endDate);

        // Load breached cases
        BreachedCases = await _slaTrackingService.GetBreachedCasesAsync();

        // Load seller statistics
        SellerStatistics = await _slaTrackingService.GetSellerStatisticsAsync(startDate, endDate);

        // Load SLA configurations
        SlaConfigurations = await _slaTrackingService.GetSlaConfigurationsAsync();
    }

    /// <summary>
    /// Gets the CSS class for a compliance percentage badge.
    /// </summary>
    /// <param name="percentage">The compliance percentage.</param>
    /// <returns>The CSS class for the badge.</returns>
    public static string GetComplianceBadgeClass(double percentage) => percentage switch
    {
        >= 95 => "bg-success",
        >= 80 => "bg-warning text-dark",
        >= 50 => "bg-danger",
        _ => "bg-dark"
    };

    /// <summary>
    /// Gets the CSS class for an SLA status badge.
    /// </summary>
    /// <param name="status">The SLA status.</param>
    /// <returns>The CSS class for the badge.</returns>
    public static string GetStatusBadgeClass(SlaStatus status) => status switch
    {
        SlaStatus.Pending => "bg-secondary",
        SlaStatus.Responded => "bg-info text-dark",
        SlaStatus.ResolvedWithinSla => "bg-success",
        SlaStatus.FirstResponseBreached => "bg-warning text-dark",
        SlaStatus.ResolutionBreached => "bg-danger",
        SlaStatus.Closed => "bg-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Formats hours as a readable duration string.
    /// </summary>
    /// <param name="hours">The number of hours.</param>
    /// <returns>A formatted duration string.</returns>
    public static string FormatDuration(double hours)
    {
        if (hours < 1)
        {
            return $"{(int)(hours * 60)}m";
        }
        if (hours < 24)
        {
            return $"{hours:F1}h";
        }
        var days = hours / 24;
        return $"{days:F1}d";
    }

    private static (DateTimeOffset StartDate, DateTimeOffset EndDate) GetDateRange(string timePeriod)
    {
        var endDate = DateTimeOffset.UtcNow;
        var startDate = timePeriod switch
        {
            "7d" => endDate.AddDays(-7),
            "30d" => endDate.AddDays(-30),
            "90d" => endDate.AddDays(-90),
            _ => endDate.AddDays(-30)
        };

        return (startDate, endDate);
    }
}
