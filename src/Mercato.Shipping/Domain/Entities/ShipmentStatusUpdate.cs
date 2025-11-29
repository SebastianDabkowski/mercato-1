namespace Mercato.Shipping.Domain.Entities;

/// <summary>
/// Represents a tracking update for a shipment.
/// </summary>
public class ShipmentStatusUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier for this status update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the shipment ID this update belongs to.
    /// </summary>
    public Guid ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the status at the time of this update.
    /// </summary>
    public ShipmentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the status message from the provider.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets the location of the shipment at the time of this update.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this status update occurred (from the provider).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this update was recorded in our system.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent shipment.
    /// </summary>
    public Shipment Shipment { get; set; } = null!;
}
