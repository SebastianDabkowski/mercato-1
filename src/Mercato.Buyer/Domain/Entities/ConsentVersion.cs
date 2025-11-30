namespace Mercato.Buyer.Domain.Entities;

/// <summary>
/// Represents a versioned text for a consent type.
/// </summary>
public class ConsentVersion
{
    /// <summary>
    /// Gets or sets the unique identifier for the consent version.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the consent type identifier.
    /// </summary>
    public Guid ConsentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the version number for this consent text.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Gets or sets the full consent text that was presented to users.
    /// </summary>
    public string ConsentText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this version became effective.
    /// </summary>
    public DateTimeOffset EffectiveFrom { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this version was superseded (null if current).
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this version was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the consent type navigation property.
    /// </summary>
    public ConsentType? ConsentType { get; set; }

    /// <summary>
    /// Gets or sets the user consents that reference this version.
    /// </summary>
    public ICollection<UserConsent> UserConsents { get; set; } = [];
}
