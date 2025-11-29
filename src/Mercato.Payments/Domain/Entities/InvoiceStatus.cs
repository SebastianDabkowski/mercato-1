namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents the status of a commission invoice.
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Invoice is being generated.</summary>
    Draft = 0,
    /// <summary>Invoice has been issued.</summary>
    Issued = 1,
    /// <summary>Invoice has been paid.</summary>
    Paid = 2,
    /// <summary>Invoice has been cancelled.</summary>
    Cancelled = 3,
    /// <summary>Invoice has been corrected by another invoice.</summary>
    Corrected = 4
}
