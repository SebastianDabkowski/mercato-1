namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents the status of an escrow entry.
/// </summary>
public enum EscrowStatus
{
    /// <summary>
    /// Funds are held in escrow pending fulfillment.
    /// </summary>
    Held = 0,

    /// <summary>
    /// Funds have been released to the seller.
    /// </summary>
    Released = 1,

    /// <summary>
    /// Funds have been fully refunded to the buyer.
    /// </summary>
    Refunded = 2,

    /// <summary>
    /// Funds have been partially refunded to the buyer.
    /// </summary>
    PartiallyRefunded = 3
}
