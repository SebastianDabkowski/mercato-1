namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents the type of a refund transaction.
/// </summary>
public enum RefundType
{
    /// <summary>
    /// Full refund of the entire order amount.
    /// </summary>
    Full = 0,

    /// <summary>
    /// Partial refund of part of the order amount.
    /// </summary>
    Partial = 1
}
