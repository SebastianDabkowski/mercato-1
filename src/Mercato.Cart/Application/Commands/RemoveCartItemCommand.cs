namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Command for removing an item from the cart.
/// </summary>
public class RemoveCartItemCommand
{
    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cart item ID to remove.
    /// </summary>
    public Guid CartItemId { get; set; }
}
