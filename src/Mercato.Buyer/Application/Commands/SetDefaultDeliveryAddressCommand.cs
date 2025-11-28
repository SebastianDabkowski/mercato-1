namespace Mercato.Buyer.Application.Commands;

/// <summary>
/// Command for setting a delivery address as the default.
/// </summary>
public class SetDefaultDeliveryAddressCommand
{
    /// <summary>
    /// Gets or sets the delivery address ID to set as default.
    /// </summary>
    public Guid AddressId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (for authorization).
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;
}
