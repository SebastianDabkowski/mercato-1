namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Query parameters for fetching user analytics data.
/// </summary>
public class UserAnalyticsQuery
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
/// Result containing aggregated user analytics metrics.
/// All data is anonymized and aggregated to comply with privacy requirements.
/// </summary>
public class UserAnalyticsResult
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
    /// Gets or sets the number of new buyer account registrations in the period.
    /// </summary>
    public int NewBuyerAccounts { get; set; }

    /// <summary>
    /// Gets or sets the number of new seller account registrations in the period.
    /// </summary>
    public int NewSellerAccounts { get; set; }

    /// <summary>
    /// Gets or sets the total number of active users in the period.
    /// A user is considered active if they logged in at least once during the period.
    /// </summary>
    public int TotalActiveUsers { get; set; }

    /// <summary>
    /// Gets or sets the count of active users grouped by role.
    /// </summary>
    public Dictionary<string, int> ActiveUsersByRole { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of users who placed at least one order in the period.
    /// </summary>
    public int UsersWhoPlacedOrders { get; set; }

    /// <summary>
    /// Gets or sets the number of users who logged in at least once in the period.
    /// </summary>
    public int UsersWhoLoggedIn { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this data was retrieved.
    /// </summary>
    public DateTimeOffset RetrievedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is insufficient data for meaningful metrics.
    /// </summary>
    public bool HasInsufficientData { get; set; }

    /// <summary>
    /// Gets or sets the message to display when there is insufficient data.
    /// </summary>
    public string? InsufficientDataMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether new buyer registration data is available.
    /// </summary>
    public bool HasBuyerRegistrationData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether new seller registration data is available.
    /// </summary>
    public bool HasSellerRegistrationData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether order activity data is available.
    /// </summary>
    public bool HasOrderActivityData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether login activity data is available.
    /// </summary>
    public bool HasLoginActivityData { get; set; }
}
