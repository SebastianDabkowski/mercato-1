namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents a commission invoice for a seller.
/// </summary>
public class CommissionInvoice
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the unique sequential invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the seller ID.</summary>
    public Guid SellerId { get; set; }

    /// <summary>Gets or sets the year of the billing period.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the month of the billing period.</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the invoice type.</summary>
    public InvoiceType InvoiceType { get; set; }

    /// <summary>Gets or sets the invoice status.</summary>
    public InvoiceStatus Status { get; set; }

    /// <summary>Gets or sets the net amount (before tax).</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Gets or sets the tax rate percentage.</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Gets or sets the tax amount.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the gross amount (after tax).</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>Gets or sets the currency code (e.g., USD, EUR).</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the issue date.</summary>
    public DateTimeOffset IssueDate { get; set; }

    /// <summary>Gets or sets the due date for payment.</summary>
    public DateTimeOffset DueDate { get; set; }

    /// <summary>Gets or sets the date when the invoice was paid.</summary>
    public DateTimeOffset? PaidDate { get; set; }

    /// <summary>Gets or sets the ID of the original invoice if this is a correction.</summary>
    public Guid? OriginalInvoiceId { get; set; }

    /// <summary>Gets or sets any notes on the invoice.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the date when this record was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the date when this record was last updated.</summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>Gets or sets the collection of line items.</summary>
    public ICollection<InvoiceLineItem> LineItems { get; set; } = [];
}
