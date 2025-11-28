namespace Mercato.Cart.Domain.Entities;

/// <summary>
/// Represents a shopping cart for a buyer or guest.
/// </summary>
public class Cart
{
    /// <summary>
    /// Gets or sets the unique identifier for the cart.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (linked to IdentityUser.Id).
    /// Null for guest carts.
    /// </summary>
    public string? BuyerId { get; set; }

    /// <summary>
    /// Gets or sets the guest cart ID (a stable identifier stored in a cookie).
    /// Null for authenticated user carts.
    /// </summary>
    public string? GuestCartId { get; set; }

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

    /// <summary>
    /// Gets a value indicating whether this is a guest cart.
    /// </summary>
    public bool IsGuestCart => string.IsNullOrEmpty(BuyerId) && !string.IsNullOrEmpty(GuestCartId);
}
