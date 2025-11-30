namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query parameters for fetching marketplace dashboard data.
/// </summary>
public class MarketplaceDashboardQuery
{
    /// <summary>
    /// Gets or sets the start date for the query period.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the query period.
    /// </summary>
    public DateTimeOffset EndDate { get; set; }
}

/// <summary>
/// Result containing marketplace performance metrics.
/// </summary>
public class MarketplaceDashboardResult
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
    /// Gets or sets the total GMV (Gross Merchandise Value) for the period.
    /// This includes order totals with shipping.
    /// </summary>
    public decimal TotalGmv { get; set; }

    /// <summary>
    /// Gets or sets the total number of orders in the period.
    /// </summary>
    public int TotalOrders { get; set; }

    /// <summary>
    /// Gets or sets the number of active sellers (stores with Active or LimitedActive status).
    /// </summary>
    public int ActiveSellers { get; set; }

    /// <summary>
    /// Gets or sets the number of active products (products with Active status).
    /// </summary>
    public int ActiveProducts { get; set; }

    /// <summary>
    /// Gets or sets the number of newly registered users in the period.
    /// </summary>
    public int NewUsers { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this data was retrieved.
    /// </summary>
    public DateTimeOffset RetrievedAt { get; set; }
}
