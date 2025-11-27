namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents a KYC (Know Your Customer) submission from a seller.
/// </summary>
public class KycSubmission
{
    /// <summary>
    /// Gets or sets the unique identifier for the KYC submission.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller's user ID (linked to IdentityUser.Id).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of document submitted.
    /// </summary>
    public KycDocumentType DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the original file name of the uploaded document.
    /// </summary>
    public string DocumentFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME content type of the document.
    /// </summary>
    public string DocumentContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document content.
    /// </summary>
    public byte[] DocumentData { get; set; } = [];

    /// <summary>
    /// Gets or sets the current status of the KYC submission.
    /// </summary>
    public KycStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the reason for rejection (if rejected).
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the submission was created.
    /// </summary>
    public DateTimeOffset SubmittedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the submission was reviewed.
    /// </summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who reviewed the submission.
    /// </summary>
    public string? ReviewedBy { get; set; }
}
