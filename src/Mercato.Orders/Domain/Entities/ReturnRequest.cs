namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a return request initiated by a buyer for a seller sub-order.
/// </summary>
public class ReturnRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the return request.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID that this return request is for.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID who initiated the return request.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the return request.
    /// </summary>
    public ReturnStatus Status { get; set; } = ReturnStatus.Requested;

    /// <summary>
    /// Gets or sets the reason provided by the buyer for the return.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the return request was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the return request was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets optional notes from the seller when approving or rejecting the return.
    /// </summary>
    public string? SellerNotes { get; set; }

    /// <summary>
    /// Navigation property to the seller sub-order being returned.
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;
}
