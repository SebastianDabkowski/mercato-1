namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Command for validating cart items before checkout completion.
/// Validates stock availability and price changes.
/// </summary>
public class ValidateCheckoutCommand
{
    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;
}
