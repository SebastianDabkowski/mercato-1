using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin;

/// <summary>
/// Page model for the security dashboard displaying authentication events and suspicious activity.
/// </summary>
[Authorize(Roles = "Admin")]
public class SecurityDashboardModel : PageModel
{
    private readonly IAuthenticationEventService _authEventService;
    private readonly ILogger<SecurityDashboardModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityDashboardModel"/> class.
    /// </summary>
    /// <param name="authEventService">The authentication event service.</param>
    /// <param name="logger">The logger.</param>
    public SecurityDashboardModel(
        IAuthenticationEventService authEventService,
        ILogger<SecurityDashboardModel> logger)
    {
        _authEventService = authEventService ?? throw new ArgumentNullException(nameof(authEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or sets the authentication statistics.
    /// </summary>
    public AuthenticationStatistics? Statistics { get; set; }

    /// <summary>
    /// Gets or sets the list of suspicious activities detected.
    /// </summary>
    public IReadOnlyList<SuspiciousActivityInfo> SuspiciousActivities { get; set; } = [];

    /// <summary>
    /// Gets or sets the recent authentication events.
    /// </summary>
    public IReadOnlyList<AuthenticationEvent> RecentEvents { get; set; } = [];

    /// <summary>
    /// Gets or sets the selected time period for filtering.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string TimePeriod { get; set; } = "24h";

    /// <summary>
    /// Gets or sets the selected event type filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public AuthenticationEventType? EventTypeFilter { get; set; }

    /// <summary>
    /// Gets or sets the selected user role filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? UserRoleFilter { get; set; }

    /// <summary>
    /// Gets or sets the selected success status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool? SuccessFilter { get; set; }

    /// <summary>
    /// Handles GET requests to load the security dashboard.
    /// </summary>
    public async Task OnGetAsync()
    {
        _logger.LogInformation("Admin accessing security dashboard with period: {TimePeriod}", TimePeriod);

        var (startDate, endDate) = GetDateRange(TimePeriod);

        // Load statistics
        Statistics = await _authEventService.GetStatisticsAsync(startDate, endDate);

        // Load suspicious activities
        SuspiciousActivities = await _authEventService.GetSuspiciousActivityAsync(startDate, endDate);

        // Load recent events with filters
        RecentEvents = await _authEventService.GetEventsAsync(
            startDate,
            endDate,
            EventTypeFilter,
            UserRoleFilter,
            SuccessFilter);
    }

    /// <summary>
    /// Gets the CSS class for an alert severity badge.
    /// </summary>
    /// <param name="severity">The alert severity.</param>
    /// <returns>The CSS class for the badge.</returns>
    public static string GetSeverityBadgeClass(AlertSeverity severity) => severity switch
    {
        AlertSeverity.Critical => "bg-danger",
        AlertSeverity.High => "bg-warning text-dark",
        AlertSeverity.Medium => "bg-info text-dark",
        AlertSeverity.Low => "bg-secondary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the CSS class for an event type badge.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>The CSS class for the badge.</returns>
    public static string GetEventTypeBadgeClass(AuthenticationEventType eventType) => eventType switch
    {
        AuthenticationEventType.Login => "bg-primary",
        AuthenticationEventType.Logout => "bg-secondary",
        AuthenticationEventType.Lockout => "bg-danger",
        AuthenticationEventType.PasswordReset => "bg-warning text-dark",
        AuthenticationEventType.PasswordChange => "bg-info text-dark",
        AuthenticationEventType.TwoFactorAuthentication => "bg-success",
        _ => "bg-secondary"
    };

    private static (DateTimeOffset StartDate, DateTimeOffset EndDate) GetDateRange(string timePeriod)
    {
        var endDate = DateTimeOffset.UtcNow;
        var startDate = timePeriod switch
        {
            "1h" => endDate.AddHours(-1),
            "6h" => endDate.AddHours(-6),
            "24h" => endDate.AddHours(-24),
            "7d" => endDate.AddDays(-7),
            "30d" => endDate.AddDays(-30),
            _ => endDate.AddHours(-24)
        };

        return (startDate, endDate);
    }
}
