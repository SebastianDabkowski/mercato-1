namespace Mercato.Payments.Application.Services;

/// <summary>
/// Settings for refund operations.
/// </summary>
public class RefundSettings
{
    /// <summary>
    /// Gets or sets the maximum number of days after payment within which a refund can be initiated.
    /// Default is 30 days.
    /// </summary>
    public int MaxRefundDaysAfterPayment { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of days after payment within which a seller can trigger a refund.
    /// Default is 14 days.
    /// </summary>
    public int SellerRefundWindowDays { get; set; } = 14;

    /// <summary>
    /// Gets or sets a value indicating whether sellers can trigger partial refunds.
    /// Default is true.
    /// </summary>
    public bool AllowSellerPartialRefunds { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether sellers can trigger full refunds.
    /// Default is false (only admins/support agents can do full refunds).
    /// </summary>
    public bool AllowSellerFullRefunds { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum percentage of the original order amount that a seller can refund.
    /// Default is 100% (no limit).
    /// </summary>
    public decimal MaxSellerRefundPercentage { get; set; } = 100m;

    /// <summary>
    /// Gets or sets a value indicating whether to log detailed provider errors.
    /// Default is true.
    /// </summary>
    public bool LogProviderErrors { get; set; } = true;
}
