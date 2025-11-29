namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents a shipping method configuration for a store.
/// </summary>
public class ShippingMethod
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipping method.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID this shipping method belongs to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the name of the shipping method (e.g., "Courier", "Parcel Locker", "Postal Service", "In-Store Pickup").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the shipping method.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of ISO country codes where this shipping method is available.
    /// If null, the method is available in all countries.
    /// </summary>
    public string? AvailableCountries { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this shipping method is active and available at checkout.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the shipping method was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the shipping method was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}
