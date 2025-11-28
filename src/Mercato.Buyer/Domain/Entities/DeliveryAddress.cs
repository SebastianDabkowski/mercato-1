namespace Mercato.Buyer.Domain.Entities;

/// <summary>
/// Represents a delivery address for a buyer.
/// </summary>
public class DeliveryAddress
{
    /// <summary>
    /// Gets or sets the unique identifier for the delivery address.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (linked to IdentityUser.Id).
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
    /// Gets or sets a value indicating whether this is the default delivery address.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the address was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the address was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}
