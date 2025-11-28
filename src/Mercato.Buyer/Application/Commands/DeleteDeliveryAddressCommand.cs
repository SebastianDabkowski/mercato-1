namespace Mercato.Buyer.Application.Commands;

/// <summary>
/// Command for deleting a delivery address.
/// </summary>
public class DeleteDeliveryAddressCommand
{
    /// <summary>
    /// Gets or sets the delivery address ID to delete.
    /// </summary>
    public Guid AddressId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (for authorization).
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;
}
