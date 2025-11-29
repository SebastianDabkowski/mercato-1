namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a shipping status change event for audit and tracking purposes.
/// Records the full history of status changes for a seller sub-order.
/// </summary>
public class ShippingStatusHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for this history record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID this status change belongs to.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the previous status before the change.
    /// </summary>
    public SellerSubOrderStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status after the change.
    /// </summary>
    public SellerSubOrderStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the status change occurred.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who made the change (seller or admin).
    /// </summary>
    public string? ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the tracking number at the time of this status change (if applicable).
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the shipping carrier at the time of this status change (if applicable).
    /// </summary>
    public string? ShippingCarrier { get; set; }

    /// <summary>
    /// Gets or sets optional notes about this status change.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property to the seller sub-order.
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;
}
