namespace Mercato.Payments.Application.Services;

/// <summary>
/// Settings for commission invoicing.
/// </summary>
public class InvoiceSettings
{
    /// <summary>Gets or sets the default tax rate percentage.</summary>
    public decimal DefaultTaxRate { get; set; } = 0m;

    /// <summary>Gets or sets the payment due days from issue date.</summary>
    public int PaymentDueDays { get; set; } = 30;

    /// <summary>Gets or sets the default currency.</summary>
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>Gets or sets the invoice number prefix.</summary>
    public string InvoiceNumberPrefix { get; set; } = "INV";
}
