namespace Mercato.Shipping.Domain.Entities;

/// <summary>
/// Represents the operational status of a shipping provider on the platform.
/// </summary>
public enum ShippingProviderStatus
{
    /// <summary>
    /// The shipping provider is active and available for use.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The shipping provider is inactive and not available for use.
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// The shipping provider is under maintenance and temporarily unavailable.
    /// </summary>
    Maintenance = 2
}
