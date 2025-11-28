namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Command for adding an item to the shopping cart.
/// </summary>
public class AddToCartCommand
{
    /// <summary>
    /// Gets or sets the buyer ID. Null for guest carts.
    /// </summary>
    public string? BuyerId { get; set; }

    /// <summary>
    /// Gets or sets the guest cart ID. Null for authenticated user carts.
    /// </summary>
    public string? GuestCartId { get; set; }

    /// <summary>
    /// Gets or sets the product ID to add.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity to add.
    /// </summary>
    public int Quantity { get; set; }
}
