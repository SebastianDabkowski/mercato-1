namespace Mercato.Cart.Application.Queries;

/// <summary>
/// Query for getting a buyer's cart.
/// </summary>
public class GetCartQuery
{
    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;
}
