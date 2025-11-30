using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for legal consent tracking.
/// </summary>
public interface ILegalConsentRepository
{
    /// <summary>
    /// Records a user's consent to a legal document version.
    /// </summary>
    /// <param name="consent">The consent record to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added consent record.</returns>
    Task<LegalConsent> AddAsync(LegalConsent consent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent consent for a user and document type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="documentType">The type of legal document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The most recent consent if found; otherwise, null.</returns>
    Task<LegalConsent?> GetLatestConsentAsync(string userId, LegalDocumentType documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all consents for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all consent records for the user.</returns>
    Task<IReadOnlyList<LegalConsent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all consents for a specific document version.
    /// </summary>
    /// <param name="legalDocumentVersionId">The version ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all consent records for the version.</returns>
    Task<IReadOnlyList<LegalConsent>> GetByVersionIdAsync(Guid legalDocumentVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has consented to a specific version.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="legalDocumentVersionId">The version ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has consented; otherwise, false.</returns>
    Task<bool> HasConsentedAsync(string userId, Guid legalDocumentVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has consented to the current active version of a document type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="documentType">The type of legal document.</param>
    /// <param name="currentVersionId">The current active version ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has consented to the current version; otherwise, false.</returns>
    Task<bool> HasConsentedToCurrentVersionAsync(string userId, LegalDocumentType documentType, Guid currentVersionId, CancellationToken cancellationToken = default);
}
