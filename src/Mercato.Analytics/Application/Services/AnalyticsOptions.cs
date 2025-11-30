namespace Mercato.Analytics.Application.Services;

/// <summary>
/// Configuration options for analytics tracking.
/// </summary>
public class AnalyticsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether analytics tracking is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days to retain analytics data.
    /// </summary>
    public int RetentionDays { get; set; } = 90;
}
