namespace Mercato.Buyer.Application.Commands;

/// <summary>
/// Command for saving (adding or updating) a delivery address.
/// </summary>
public class SaveDeliveryAddressCommand
{
    /// <summary>
    /// Gets or sets the delivery address ID. Null for new addresses.
    /// </summary>
    public Guid? AddressId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the label for the address (e.g., "Home", "Work").
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the full name of the recipient.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary address line.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary address line (optional).
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the postal or ZIP code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO country code (e.g., "US", "CA", "GB").
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number (optional).
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set this as the default address.
    /// </summary>
    public bool SetAsDefault { get; set; }
}
