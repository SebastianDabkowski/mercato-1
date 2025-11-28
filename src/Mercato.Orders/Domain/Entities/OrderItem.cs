namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents an item within an order, containing a snapshot of product details
/// at the time of order creation.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the order item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID this item belongs to.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the product ID (reference to the original product).
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the product title (snapshotted at order creation).
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price (snapshotted at order creation).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the quantity ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the store name (snapshotted at order creation).
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product image URL (snapshotted at order creation).
    /// </summary>
    public string? ProductImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets the total price for this item (quantity × unit price).
    /// </summary>
    public decimal TotalPrice => UnitPrice * Quantity;

    /// <summary>
    /// Navigation property to the parent order.
    /// </summary>
    public Order Order { get; set; } = null!;
}
