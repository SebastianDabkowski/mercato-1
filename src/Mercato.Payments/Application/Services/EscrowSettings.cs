namespace Mercato.Payments.Application.Services;

/// <summary>
/// Configuration settings for escrow operations.
/// </summary>
public class EscrowSettings
{
    /// <summary>
    /// Gets or sets the number of days after order completion before funds are eligible for payout.
    /// </summary>
    public int PayoutEligibilityDays { get; set; } = 14;

    /// <summary>
    /// Gets or sets the marketplace commission percentage (0-100).
    /// </summary>
    public decimal CommissionPercentage { get; set; } = 10.0m;
}
