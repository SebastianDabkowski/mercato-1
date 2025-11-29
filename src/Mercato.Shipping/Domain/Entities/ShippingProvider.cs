namespace Mercato.Shipping.Domain.Entities;

/// <summary>
/// Represents a platform-level shipping provider that can be used by sellers.
/// </summary>
public class ShippingProvider
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipping provider.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the shipping provider.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique code identifying the provider (e.g., "DHL", "FEDEX", "UPS").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the shipping provider.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is active on the platform.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the current operational status of the provider.
    /// </summary>
    public ShippingProviderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the base API endpoint URL for the provider's API.
    /// </summary>
    public string? ApiEndpointUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL for the provider's logo or icon.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the base URL for tracking shipments with this provider.
    /// </summary>
    public string? TrackingUrlTemplate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the provider was added to the platform.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the provider was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}
