namespace Mercato.Identity.Application.Queries;

/// <summary>
/// Represents a complete export of user personal data for GDPR compliance.
/// </summary>
public class UserDataExport
{
    /// <summary>
    /// Gets or sets the timestamp when this export was generated.
    /// </summary>
    public DateTimeOffset ExportedAt { get; set; }

    /// <summary>
    /// Gets or sets the user's identity information.
    /// </summary>
    public UserIdentityData? Identity { get; set; }

    /// <summary>
    /// Gets or sets the user's delivery addresses (for buyers).
    /// </summary>
    public IReadOnlyList<DeliveryAddressData> DeliveryAddresses { get; set; } = [];

    /// <summary>
    /// Gets or sets the user's order history (for buyers).
    /// </summary>
    public IReadOnlyList<OrderData> Orders { get; set; } = [];

    /// <summary>
    /// Gets or sets the user's store information (for sellers).
    /// </summary>
    public StoreData? Store { get; set; }

    /// <summary>
    /// Gets or sets the user's consent records.
    /// </summary>
    public IReadOnlyList<ConsentData> Consents { get; set; } = [];
}

/// <summary>
/// Represents the user's identity data from ASP.NET Core Identity.
/// </summary>
public class UserIdentityData
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the email is confirmed.
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether two-factor authentication is enabled.
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the account is locked out.
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// Gets or sets the lockout end date if locked out.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets the linked external login providers.
    /// </summary>
    public IReadOnlyList<ExternalLoginData> ExternalLogins { get; set; } = [];
}

/// <summary>
/// Represents external login provider information.
/// </summary>
public class ExternalLoginData
{
    /// <summary>
    /// Gets or sets the login provider name (e.g., "Google", "Facebook").
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// Gets or sets the display name of the provider.
    /// </summary>
    public string? ProviderDisplayName { get; set; }
}

/// <summary>
/// Represents a delivery address for data export.
/// </summary>
public class DeliveryAddressData
{
    /// <summary>
    /// Gets or sets the label for the address.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the full name of the recipient.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the primary address line.
    /// </summary>
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Gets or sets the secondary address line.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the state or province.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default address.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets when the address was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Represents order data for data export.
/// </summary>
public class OrderData
{
    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    public string? OrderNumber { get; set; }

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets when the order was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the delivery full name.
    /// </summary>
    public string? DeliveryFullName { get; set; }

    /// <summary>
    /// Gets or sets the delivery address.
    /// </summary>
    public string? DeliveryAddress { get; set; }

    /// <summary>
    /// Gets or sets the delivery city.
    /// </summary>
    public string? DeliveryCity { get; set; }

    /// <summary>
    /// Gets or sets the delivery country.
    /// </summary>
    public string? DeliveryCountry { get; set; }

    /// <summary>
    /// Gets or sets the buyer email used for the order.
    /// </summary>
    public string? BuyerEmail { get; set; }

    /// <summary>
    /// Gets or sets the order items.
    /// </summary>
    public IReadOnlyList<OrderItemData> Items { get; set; } = [];
}

/// <summary>
/// Represents order item data for data export.
/// </summary>
public class OrderItemData
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price at time of order.
    /// </summary>
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Represents store data for seller data export.
/// </summary>
public class StoreData
{
    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the store description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the contact email.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact phone.
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the website URL.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets when the store was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the store status.
    /// </summary>
    public string? Status { get; set; }
}

/// <summary>
/// Represents consent record data for data export.
/// </summary>
public class ConsentData
{
    /// <summary>
    /// Gets or sets the consent type name.
    /// </summary>
    public string? ConsentType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether consent was granted.
    /// </summary>
    public bool IsGranted { get; set; }

    /// <summary>
    /// Gets or sets when the consent was given or revoked.
    /// </summary>
    public DateTimeOffset ConsentDate { get; set; }
}
