using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for submitting a KYC document.
/// </summary>
public class SubmitKycCommand
{
    /// <summary>
    /// Gets or sets the seller's user ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of document being submitted.
    /// </summary>
    public KycDocumentType DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the original file name of the document.
    /// </summary>
    public string DocumentFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME content type of the document.
    /// </summary>
    public string DocumentContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document content (max 5MB).
    /// </summary>
    public byte[] DocumentData { get; set; } = [];
}
