namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a user's consent to a specific version of a legal document.
/// Used for audit and compliance tracking.
/// </summary>
public class LegalConsent
{
    /// <summary>
    /// Gets or sets the unique identifier for this consent record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who gave consent.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the legal document version that was consented to.
    /// </summary>
    public Guid LegalDocumentVersionId { get; set; }

    /// <summary>
    /// Gets or sets the type of the legal document that was consented to.
    /// Denormalized for query efficiency.
    /// </summary>
    public LegalDocumentType DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the version number that was consented to.
    /// Denormalized for audit purposes.
    /// </summary>
    public string VersionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when consent was given.
    /// </summary>
    public DateTimeOffset ConsentedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address (hashed for privacy) from which consent was given.
    /// </summary>
    public string? IpAddressHash { get; set; }

    /// <summary>
    /// Gets or sets the context in which consent was given (e.g., "Registration", "Checkout").
    /// </summary>
    public string ConsentContext { get; set; } = string.Empty;
}
