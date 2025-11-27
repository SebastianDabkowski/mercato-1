namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents a seller's store profile with contact details and branding.
/// </summary>
public class Store
{
    /// <summary>
    /// Gets or sets the unique identifier for the store.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller's user ID (linked to IdentityUser.Id).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SEO-friendly URL slug for the store.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store status for visibility control.
    /// </summary>
    public StoreStatus Status { get; set; } = StoreStatus.PendingVerification;

    /// <summary>
    /// Gets or sets the store description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the store logo URL.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the contact email address for the store.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact phone number for the store.
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the website URL for the store.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the store was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the store was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}
