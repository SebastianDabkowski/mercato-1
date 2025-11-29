namespace Mercato.Shipping.Domain.Entities;

/// <summary>
/// Represents a seller's enabled shipping provider for their store.
/// </summary>
public class StoreShippingProvider
{
    /// <summary>
    /// Gets or sets the unique identifier for this store-provider relationship.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID that has enabled this shipping provider.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the shipping provider ID.
    /// </summary>
    public Guid ShippingProviderId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this provider is enabled for the store.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a secure reference identifier for the seller's credentials with this provider.
    /// This is used to look up encrypted credentials stored securely elsewhere.
    /// </summary>
    public string? CredentialIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the seller's account number or ID with the shipping provider.
    /// </summary>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this provider was enabled for the store.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this configuration was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the shipping provider.
    /// </summary>
    public ShippingProvider ShippingProvider { get; set; } = null!;
}
