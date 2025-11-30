namespace Mercato.Buyer.Domain.Entities;

/// <summary>
/// Represents a user's consent decision for a specific consent version.
/// </summary>
public class UserConsent
{
    /// <summary>
    /// Gets or sets the unique identifier for this consent record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID (linked to IdentityUser.Id).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consent version identifier.
    /// </summary>
    public Guid ConsentVersionId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether consent was granted.
    /// </summary>
    public bool IsGranted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the consent decision was made.
    /// </summary>
    public DateTimeOffset ConsentedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the consent was given (for audit purposes).
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string (for audit purposes).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the consent version navigation property.
    /// </summary>
    public ConsentVersion? ConsentVersion { get; set; }
}
