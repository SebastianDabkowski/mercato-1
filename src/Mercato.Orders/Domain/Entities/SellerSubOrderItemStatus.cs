namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the status of an individual item within a seller sub-order.
/// Enables partial fulfillment by tracking each item's lifecycle independently.
/// </summary>
public enum SellerSubOrderItemStatus
{
    /// <summary>
    /// The item is newly added to the sub-order.
    /// </summary>
    New = 0,

    /// <summary>
    /// The item is being prepared for shipment.
    /// </summary>
    Preparing = 1,

    /// <summary>
    /// The item has been shipped.
    /// </summary>
    Shipped = 2,

    /// <summary>
    /// The item has been delivered.
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// The item has been cancelled by the seller.
    /// </summary>
    Cancelled = 4
}
