namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Command for adding an item to the shopping cart.
/// </summary>
public class AddToCartCommand
{
    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product ID to add.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity to add.
    /// </summary>
    public int Quantity { get; set; }
}
