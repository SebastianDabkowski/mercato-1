namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a data processing activity record as required by GDPR Article 30.
/// This entity maintains the registry of processing activities for compliance purposes.
/// </summary>
public class DataProcessingActivity
{
    /// <summary>
    /// Gets or sets the unique identifier for this processing activity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the processing activity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose(s) of the processing.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal basis for processing (e.g., Consent, Contract, Legal Obligation, Vital Interests, Public Task, Legitimate Interests).
    /// </summary>
    public string LegalBasis { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of personal data being processed.
    /// </summary>
    public string DataCategories { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of data subjects (e.g., Customers, Employees, Website visitors).
    /// </summary>
    public string DataSubjectCategories { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipients or categories of recipients to whom data is disclosed.
    /// </summary>
    public string Recipients { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets information about transfers to third countries or international organizations.
    /// </summary>
    public string? ThirdCountryTransfers { get; set; }

    /// <summary>
    /// Gets or sets the retention period description for erasing data.
    /// </summary>
    public string RetentionPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the technical security measures in place.
    /// </summary>
    public string? TechnicalMeasures { get; set; }

    /// <summary>
    /// Gets or sets a description of the organizational security measures in place.
    /// </summary>
    public string? OrganizationalMeasures { get; set; }

    /// <summary>
    /// Gets or sets the name of the data processor, if applicable.
    /// </summary>
    public string? ProcessorName { get; set; }

    /// <summary>
    /// Gets or sets the contact information for the data processor.
    /// </summary>
    public string? ProcessorContact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this processing activity is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this record.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this record was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this record.
    /// </summary>
    public string? UpdatedByUserId { get; set; }
}
