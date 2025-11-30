using Mercato.Seller.Application.Queries;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller;

/// <summary>
/// Page model for the seller sales dashboard displaying sales metrics and charts.
/// </summary>
[Authorize(Roles = "Seller")]
public class SalesDashboardModel : PageModel
{
    private readonly ISellerSalesDashboardService _dashboardService;
    private readonly IStoreRepository _storeRepository;
    private readonly ILogger<SalesDashboardModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesDashboardModel"/> class.
    /// </summary>
    /// <param name="dashboardService">The seller sales dashboard service.</param>
    /// <param name="storeRepository">The store repository.</param>
    /// <param name="logger">The logger.</param>
    public SalesDashboardModel(
        ISellerSalesDashboardService dashboardService,
        IStoreRepository storeRepository,
        ILogger<SalesDashboardModel> logger)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _dashboardService = dashboardService;
        _storeRepository = storeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the dashboard result containing sales metrics and chart data.
    /// </summary>
    public SellerSalesDashboardResult? DashboardResult { get; set; }

    /// <summary>
    /// Gets or sets the selected time period for filtering.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string TimePeriod { get; set; } = "30d";

    /// <summary>
    /// Gets or sets the selected granularity for chart data.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Granularity { get; set; } = "day";

    /// <summary>
    /// Gets or sets the optional product ID filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the optional category filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the store name for display.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether there is no data for the selected period.
    /// </summary>
    public bool HasNoData => DashboardResult != null &&
        DashboardResult.TotalGmv == 0 &&
        DashboardResult.TotalOrders == 0;

    /// <summary>
    /// Handles GET requests to load the sales dashboard.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims when accessing sales dashboard");
            return Forbid();
        }

        var store = await _storeRepository.GetBySellerIdAsync(sellerId);
        if (store == null)
        {
            _logger.LogWarning("Seller {SellerId} does not have a store when accessing sales dashboard", sellerId);
            return Forbid();
        }

        StoreName = store.Name;
        _logger.LogInformation("Seller {SellerId} accessing sales dashboard for store {StoreId} with period: {TimePeriod}, granularity: {Granularity}",
            sellerId, store.Id, TimePeriod, Granularity);

        var (startDate, endDate) = GetDateRange(TimePeriod);
        var granularity = ParseGranularity(Granularity);

        var query = new SellerSalesDashboardQuery
        {
            StoreId = store.Id,
            StartDate = startDate,
            EndDate = endDate,
            Granularity = granularity,
            ProductId = ProductId,
            Category = Category
        };

        DashboardResult = await _dashboardService.GetDashboardAsync(query);

        return Page();
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

    /// <summary>
    /// Gets the display text for a granularity.
    /// </summary>
    /// <param name="granularity">The granularity code.</param>
    /// <returns>The display text.</returns>
    public static string GetGranularityDisplay(string granularity) => granularity switch
    {
        "day" => "Daily",
        "week" => "Weekly",
        "month" => "Monthly",
        _ => "Daily"
    };

    /// <summary>
    /// Gets the chart data as JSON for Chart.js.
    /// </summary>
    /// <returns>JSON string of chart data.</returns>
    public string GetChartLabelsJson()
    {
        if (DashboardResult?.ChartDataPoints == null || DashboardResult.ChartDataPoints.Count == 0)
            return "[]";

        var labels = DashboardResult.ChartDataPoints.Select(dp => dp.Date.ToString("MMM dd"));
        return System.Text.Json.JsonSerializer.Serialize(labels);
    }

    /// <summary>
    /// Gets the GMV data as JSON for Chart.js.
    /// </summary>
    /// <returns>JSON string of GMV values.</returns>
    public string GetGmvDataJson()
    {
        if (DashboardResult?.ChartDataPoints == null || DashboardResult.ChartDataPoints.Count == 0)
            return "[]";

        var values = DashboardResult.ChartDataPoints.Select(dp => dp.Gmv);
        return System.Text.Json.JsonSerializer.Serialize(values);
    }

    /// <summary>
    /// Gets the order count data as JSON for Chart.js.
    /// </summary>
    /// <returns>JSON string of order count values.</returns>
    public string GetOrderCountDataJson()
    {
        if (DashboardResult?.ChartDataPoints == null || DashboardResult.ChartDataPoints.Count == 0)
            return "[]";

        var values = DashboardResult.ChartDataPoints.Select(dp => dp.OrderCount);
        return System.Text.Json.JsonSerializer.Serialize(values);
    }

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

    private static SalesGranularity ParseGranularity(string granularity) => granularity switch
    {
        "day" => SalesGranularity.Day,
        "week" => SalesGranularity.Week,
        "month" => SalesGranularity.Month,
        _ => SalesGranularity.Day
    };
}
