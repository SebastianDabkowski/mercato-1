namespace Mercato.Cart.Domain.Entities;

/// <summary>
/// Represents an item in a shopping cart.
/// </summary>
public class CartItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the cart item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the cart ID this item belongs to.
    /// </summary>
    public Guid CartId { get; set; }

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID (for grouping by seller).
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the quantity of this item.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the product title (snapshotted at add time).
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price (snapshotted at add time).
    /// </summary>
    public decimal ProductPrice { get; set; }

    /// <summary>
    /// Gets or sets the product image URL (snapshotted at add time).
    /// </summary>
    public string? ProductImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the store name (snapshotted at add time for display).
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the item was added.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the cart.
    /// </summary>
    public Cart Cart { get; set; } = null!;
}
