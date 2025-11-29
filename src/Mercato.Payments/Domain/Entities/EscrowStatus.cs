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
    /// Funds have been refunded to the buyer.
    /// </summary>
    Refunded = 2
}
