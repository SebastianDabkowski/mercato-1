namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Command to remove a promo code from a cart.
/// </summary>
public class RemovePromoCodeCommand
{
    /// <summary>
    /// Gets or sets the buyer ID. Null for guest carts.
    /// </summary>
    public string? BuyerId { get; set; }

    /// <summary>
    /// Gets or sets the guest cart ID. Null for authenticated users.
    /// </summary>
    public string? GuestCartId { get; set; }
}
