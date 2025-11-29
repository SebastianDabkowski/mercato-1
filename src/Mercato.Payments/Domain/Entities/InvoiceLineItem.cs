namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents a line item on a commission invoice.
/// </summary>
public class InvoiceLineItem
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the invoice ID.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>Gets or sets the commission record ID.</summary>
    public Guid? CommissionRecordId { get; set; }

    /// <summary>Gets or sets the description of the line item.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the quantity (typically 1 for commissions).</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets or sets the unit price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the net amount.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Gets or sets the navigation property to the invoice.</summary>
    public CommissionInvoice Invoice { get; set; } = null!;
}
