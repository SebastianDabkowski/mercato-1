namespace Mercato.Payments.Application.Services;

/// <summary>
/// Configuration settings for payout operations.
/// </summary>
public class PayoutSettings
{
    /// <summary>
    /// Gets or sets the minimum balance threshold for payouts.
    /// Balances below this threshold roll over to the next payout period.
    /// </summary>
    public decimal MinimumPayoutThreshold { get; set; } = 25.00m;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed payouts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the day of the week for weekly payouts (0 = Sunday, 6 = Saturday).
    /// </summary>
    public int WeeklyPayoutDayOfWeek { get; set; } = 1; // Monday

    /// <summary>
    /// Gets or sets whether payout batching is enabled.
    /// </summary>
    public bool EnableBatching { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of payouts per batch.
    /// </summary>
    public int MaxPayoutsPerBatch { get; set; } = 100;

    /// <summary>
    /// Gets or sets the default currency for payouts.
    /// </summary>
    public string DefaultCurrency { get; set; } = "USD";
}
