namespace Mercato.Payments.Application.Services;

/// <summary>
/// Configuration settings for payment methods.
/// </summary>
public class PaymentSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether credit card payments are enabled.
    /// </summary>
    public bool EnableCreditCard { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether PayPal payments are enabled.
    /// </summary>
    public bool EnablePayPal { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether bank transfer payments are enabled.
    /// </summary>
    public bool EnableBankTransfer { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether BLIK payments are enabled.
    /// </summary>
    public bool EnableBlik { get; set; } = true;
}
