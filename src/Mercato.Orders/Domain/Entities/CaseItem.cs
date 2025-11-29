namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents an item included in a return/complaint case.
/// Links a specific seller sub-order item to a return request.
/// </summary>
public class CaseItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the case item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the return request ID this item belongs to.
    /// </summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order item ID this case item refers to.
    /// </summary>
    public Guid SellerSubOrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the quantity of items included in the case.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the case item was added.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent return request.
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;

    /// <summary>
    /// Navigation property to the seller sub-order item.
    /// </summary>
    public SellerSubOrderItem SellerSubOrderItem { get; set; } = null!;
}
