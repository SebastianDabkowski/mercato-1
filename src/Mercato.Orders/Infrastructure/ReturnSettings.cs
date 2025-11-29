namespace Mercato.Orders.Infrastructure;

/// <summary>
/// Configuration settings for return request functionality.
/// </summary>
public class ReturnSettings
{
    /// <summary>
    /// Gets or sets the number of days after delivery within which a return can be initiated.
    /// Default is 30 days.
    /// </summary>
    public int ReturnWindowDays { get; set; } = 30;
}
