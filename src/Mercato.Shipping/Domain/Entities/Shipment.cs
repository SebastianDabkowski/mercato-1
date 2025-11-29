namespace Mercato.Shipping.Domain.Entities;

/// <summary>
/// Represents a shipment created via a shipping provider integration.
/// </summary>
public class Shipment
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID this shipment is associated with.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the store shipping provider ID used for this shipment.
    /// </summary>
    public Guid StoreShippingProviderId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number assigned by the shipping provider.
    /// </summary>
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external shipment ID from the provider's system.
    /// </summary>
    public string? ExternalShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the current status of the shipment.
    /// </summary>
    public ShipmentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets a human-readable status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the estimated delivery date.
    /// </summary>
    public DateTimeOffset? EstimatedDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the URL for the shipping label (if available).
    /// </summary>
    public string? LabelUrl { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the shipment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the shipment was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the shipment was shipped (picked up by carrier).
    /// </summary>
    public DateTimeOffset? ShippedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the shipment was delivered.
    /// </summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>
    /// Navigation property to the store shipping provider used.
    /// </summary>
    public StoreShippingProvider StoreShippingProvider { get; set; } = null!;

    /// <summary>
    /// Navigation property to the shipment status updates.
    /// </summary>
    public ICollection<ShipmentStatusUpdate> StatusUpdates { get; set; } = new List<ShipmentStatusUpdate>();
}
