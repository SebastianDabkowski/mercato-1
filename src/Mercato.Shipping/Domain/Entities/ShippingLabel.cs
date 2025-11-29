namespace Mercato.Shipping.Domain.Entities;

/// <summary>
/// Represents a shipping label stored for a shipment.
/// </summary>
public class ShippingLabel
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipping label.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the shipment ID this label belongs to.
    /// </summary>
    public Guid ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the binary data of the label (e.g., PDF content).
    /// </summary>
    public byte[] LabelData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the content type of the label (e.g., "application/pdf").
    /// </summary>
    public string ContentType { get; set; } = "application/pdf";

    /// <summary>
    /// Gets or sets the file name for the label.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the format of the label (e.g., "PDF", "PNG", "ZPL").
    /// </summary>
    public string? LabelFormat { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the label was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the label expires (if applicable).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Navigation property to the parent shipment.
    /// </summary>
    public Shipment Shipment { get; set; } = null!;
}
