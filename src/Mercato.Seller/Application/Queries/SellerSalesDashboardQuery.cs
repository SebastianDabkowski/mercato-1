namespace Mercato.Seller.Application.Queries;

/// <summary>
/// Represents the time granularity for sales data aggregation.
/// </summary>
public enum SalesGranularity
{
    /// <summary>
    /// Group sales data by day.
    /// </summary>
    Day,

    /// <summary>
    /// Group sales data by week.
    /// </summary>
    Week,

    /// <summary>
    /// Group sales data by month.
    /// </summary>
    Month
}

/// <summary>
/// Query parameters for fetching seller sales dashboard data.
/// </summary>
public class SellerSalesDashboardQuery
{
    /// <summary>
    /// Gets or sets the store ID for which to retrieve sales data.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the start date for the query period.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the query period.
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Gets or sets the time granularity for chart data aggregation.
    /// </summary>
    public SalesGranularity Granularity { get; set; } = SalesGranularity.Day;

    /// <summary>
    /// Gets or sets the optional product ID filter.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the optional category filter.
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Represents a single data point for the sales chart.
/// </summary>
public class SalesChartDataPoint
{
    /// <summary>
    /// Gets or sets the date for this data point.
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Gets or sets the GMV (Gross Merchandise Value) for this period.
    /// </summary>
    public decimal Gmv { get; set; }

    /// <summary>
    /// Gets or sets the number of orders for this period.
    /// </summary>
    public int OrderCount { get; set; }
}

/// <summary>
/// Represents a product available for filtering in the dashboard.
/// </summary>
public class ProductFilterItem
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Result containing seller sales dashboard data.
/// </summary>
public class SellerSalesDashboardResult
{
    /// <summary>
    /// Gets or sets the start date of the period.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the period.
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this data was retrieved.
    /// </summary>
    public DateTimeOffset RetrievedAt { get; set; }

    /// <summary>
    /// Gets or sets the total GMV (Gross Merchandise Value) for the period.
    /// This includes items subtotal plus shipping costs.
    /// </summary>
    public decimal TotalGmv { get; set; }

    /// <summary>
    /// Gets or sets the total number of sub-orders in the period.
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Gets or sets the chart data points for the period.
    /// </summary>
    public IReadOnlyList<SalesChartDataPoint> ChartDataPoints { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of products available for filtering.
    /// </summary>
    public IReadOnlyList<ProductFilterItem> AvailableProducts { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of categories available for filtering.
    /// </summary>
    public IReadOnlyList<string> AvailableCategories { get; set; } = [];
}
