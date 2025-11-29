namespace Mercato.Shipping.Domain.Entities;

/// <summary>
/// Represents the status of a shipment through its lifecycle.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>
    /// The shipment has been created but not yet picked up by the carrier.
    /// </summary>
    Created = 0,

    /// <summary>
    /// The shipment has been picked up by the carrier.
    /// </summary>
    PickedUp = 1,

    /// <summary>
    /// The shipment is in transit to the destination.
    /// </summary>
    InTransit = 2,

    /// <summary>
    /// The shipment is out for delivery to the final destination.
    /// </summary>
    OutForDelivery = 3,

    /// <summary>
    /// The shipment has been delivered successfully.
    /// </summary>
    Delivered = 4,

    /// <summary>
    /// An exception occurred during shipment (e.g., delivery attempt failed, address issue).
    /// </summary>
    Exception = 5,

    /// <summary>
    /// The shipment has been returned to the sender.
    /// </summary>
    Returned = 6
}
