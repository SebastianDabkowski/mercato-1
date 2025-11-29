namespace Mercato.Payments.Application.Services;

/// <summary>
/// Configuration settings for commission operations.
/// </summary>
public class CommissionSettings
{
    /// <summary>
    /// Gets or sets the default commission rate as a percentage (e.g., 10.0 for 10%).
    /// This rate is used when no matching commission rule is found.
    /// </summary>
    public decimal DefaultCommissionRate { get; set; } = 10.0m;
}
