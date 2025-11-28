namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Command for updating the quantity of a cart item.
/// </summary>
public class UpdateCartItemQuantityCommand
{
    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cart item ID to update.
    /// </summary>
    public Guid CartItemId { get; set; }

    /// <summary>
    /// Gets or sets the new quantity.
    /// </summary>
    public int Quantity { get; set; }
}
