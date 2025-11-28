namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the status of a seller sub-order throughout its lifecycle.
/// </summary>
public enum SellerSubOrderStatus
{
    /// <summary>
    /// The sub-order has been created but payment is pending.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment has been confirmed and the sub-order is ready for fulfillment.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// The sub-order is being prepared for shipment.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// The sub-order has been shipped.
    /// </summary>
    Shipped = 3,

    /// <summary>
    /// The sub-order has been delivered.
    /// </summary>
    Delivered = 4,

    /// <summary>
    /// The sub-order has been cancelled.
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// The sub-order has been refunded.
    /// </summary>
    Refunded = 6
}
