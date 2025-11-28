namespace Mercato.Cart.Domain.Entities;

/// <summary>
/// Represents a shopping cart for a buyer.
/// </summary>
public class Cart
{
    /// <summary>
    /// Gets or sets the unique identifier for the cart.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (linked to IdentityUser.Id).
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the cart was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the cart was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the cart items.
    /// </summary>
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
