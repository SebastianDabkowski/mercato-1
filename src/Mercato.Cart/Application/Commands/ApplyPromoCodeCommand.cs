namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Command to apply a promo code to a cart.
/// </summary>
public class ApplyPromoCodeCommand
{
    /// <summary>
    /// Gets or sets the buyer ID. Null for guest carts.
    /// </summary>
    public string? BuyerId { get; set; }

    /// <summary>
    /// Gets or sets the guest cart ID. Null for authenticated users.
    /// </summary>
    public string? GuestCartId { get; set; }

    /// <summary>
    /// Gets or sets the promo code string to apply.
    /// </summary>
    public string PromoCode { get; set; } = string.Empty;
}
