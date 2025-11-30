namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a legal document that can have multiple versions over time.
/// </summary>
public class LegalDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for the legal document.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the legal document.
    /// </summary>
    public LegalDocumentType DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the display title of the document.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a brief description of the document.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this document is active.
    /// Inactive documents are not shown to users.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when this document was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this document.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this document was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this document.
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
