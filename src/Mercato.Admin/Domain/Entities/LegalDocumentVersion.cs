namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a specific version of a legal document with its content and effective date.
/// </summary>
public class LegalDocumentVersion
{
    /// <summary>
    /// Gets or sets the unique identifier for this version.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the parent legal document.
    /// </summary>
    public Guid LegalDocumentId { get; set; }

    /// <summary>
    /// Gets or sets the version number (e.g., "1.0", "2.0", "2.1").
    /// </summary>
    public string VersionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTML content of this version.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this version becomes effective.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this version is published.
    /// Only published versions can become active.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Gets or sets an optional summary of changes from the previous version.
    /// </summary>
    public string? ChangeSummary { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this version was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this version.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this version was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this version.
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
