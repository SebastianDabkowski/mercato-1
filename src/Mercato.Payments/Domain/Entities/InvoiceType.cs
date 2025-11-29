namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents the type of a commission invoice.
/// </summary>
public enum InvoiceType
{
    /// <summary>Standard commission invoice.</summary>
    Standard = 0,
    /// <summary>Credit note (correction reducing amount).</summary>
    CreditNote = 1,
    /// <summary>Correction invoice.</summary>
    Correction = 2
}
