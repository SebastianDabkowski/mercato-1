namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the status of an order throughout its lifecycle.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// The order has been created but payment is pending.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment has been confirmed and the order is being processed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// The order is being prepared for shipment.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// The order has been shipped.
    /// </summary>
    Shipped = 3,

    /// <summary>
    /// The order has been delivered.
    /// </summary>
    Delivered = 4,

    /// <summary>
    /// The order has been cancelled.
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// The order has been refunded.
    /// </summary>
    Refunded = 6
}
