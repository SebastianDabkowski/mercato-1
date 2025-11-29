namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a seller-specific sub-order that is part of a parent order.
/// Each seller in a multi-seller order receives their own sub-order to manage fulfillment.
/// </summary>
public class SellerSubOrder
{
    /// <summary>
    /// Gets or sets the unique identifier for the seller sub-order.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent order ID.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the store ID (seller's store).
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store name (snapshotted at order creation).
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sub-order number for display purposes.
    /// </summary>
    public string SubOrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the seller sub-order.
    /// </summary>
    public SellerSubOrderStatus Status { get; set; } = SellerSubOrderStatus.New;

    /// <summary>
    /// Gets or sets the subtotal for items in this sub-order.
    /// </summary>
    public decimal ItemsSubtotal { get; set; }

    /// <summary>
    /// Gets or sets the shipping cost for this sub-order.
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets the total amount for this sub-order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order was confirmed.
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order was shipped.
    /// </summary>
    public DateTimeOffset? ShippedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order was delivered.
    /// </summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order was refunded.
    /// </summary>
    public DateTimeOffset? RefundedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the sub-order payment failed.
    /// </summary>
    public DateTimeOffset? FailedAt { get; set; }

    /// <summary>
    /// Gets or sets the tracking number for shipment.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the shipping carrier name.
    /// </summary>
    public string? ShippingCarrier { get; set; }

    /// <summary>
    /// Gets or sets the shipping method name chosen by the buyer.
    /// </summary>
    public string? ShippingMethodName { get; set; }

    /// <summary>
    /// Navigation property to the parent order.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property to the items in this seller sub-order.
    /// </summary>
    public ICollection<SellerSubOrderItem> Items { get; set; } = new List<SellerSubOrderItem>();

    /// <summary>
    /// Gets the total price of cancelled items for refund calculation.
    /// </summary>
    public decimal CancelledItemsTotal => Items
        .Where(i => i.Status == SellerSubOrderItemStatus.Cancelled)
        .Sum(i => i.TotalPrice);

    /// <summary>
    /// Gets the total price of shipped items.
    /// </summary>
    public decimal ShippedItemsTotal => Items
        .Where(i => i.Status == SellerSubOrderItemStatus.Shipped || i.Status == SellerSubOrderItemStatus.Delivered)
        .Sum(i => i.TotalPrice);

    /// <summary>
    /// Gets a value indicating whether the sub-order is partially fulfilled.
    /// </summary>
    public bool IsPartiallyFulfilled => Items.Any() &&
        Items.Any(i => i.Status == SellerSubOrderItemStatus.Shipped || i.Status == SellerSubOrderItemStatus.Delivered) &&
        Items.Any(i => i.Status != SellerSubOrderItemStatus.Shipped && i.Status != SellerSubOrderItemStatus.Delivered);
}
