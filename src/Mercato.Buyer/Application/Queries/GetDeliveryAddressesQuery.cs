namespace Mercato.Buyer.Application.Queries;

/// <summary>
/// Query for getting delivery addresses for a buyer.
/// </summary>
public class GetDeliveryAddressesQuery
{
    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;
}
