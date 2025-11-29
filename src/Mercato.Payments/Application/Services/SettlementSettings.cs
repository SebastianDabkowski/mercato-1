namespace Mercato.Payments.Application.Services;

/// <summary>
/// Configuration settings for settlement operations.
/// </summary>
public class SettlementSettings
{
    /// <summary>
    /// Gets or sets the day of the month when settlements are generated (1-28).
    /// </summary>
    public int GenerationDayOfMonth { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to automatically generate settlements.
    /// </summary>
    public bool AutoGenerateSettlements { get; set; } = true;

    /// <summary>
    /// Gets or sets the default currency for settlements.
    /// </summary>
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the number of months to look back for adjustments.
    /// </summary>
    public int AdjustmentLookbackMonths { get; set; } = 3;
}
