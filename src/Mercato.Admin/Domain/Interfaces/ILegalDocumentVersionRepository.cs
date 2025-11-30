using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for legal document version management.
/// </summary>
public interface ILegalDocumentVersionRepository
{
    /// <summary>
    /// Gets a legal document version by its unique identifier.
    /// </summary>
    /// <param name="id">The version ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version if found; otherwise, null.</returns>
    Task<LegalDocumentVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions for a specific legal document.
    /// </summary>
    /// <param name="legalDocumentId">The legal document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all versions for the document, ordered by effective date descending.</returns>
    Task<IReadOnlyList<LegalDocumentVersion>> GetByDocumentIdAsync(Guid legalDocumentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active (effective) version for a legal document.
    /// Returns the most recent published version with an effective date in the past or present.
    /// </summary>
    /// <param name="legalDocumentId">The legal document ID.</param>
    /// <param name="asOfDate">The date to check for effectiveness.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active version if found; otherwise, null.</returns>
    Task<LegalDocumentVersion?> GetActiveVersionAsync(Guid legalDocumentId, DateTimeOffset asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active version for a specific document type.
    /// </summary>
    /// <param name="documentType">The type of legal document.</param>
    /// <param name="asOfDate">The date to check for effectiveness.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active version if found; otherwise, null.</returns>
    Task<LegalDocumentVersion?> GetActiveVersionByTypeAsync(LegalDocumentType documentType, DateTimeOffset asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets any upcoming version (effective date in the future) for a legal document.
    /// </summary>
    /// <param name="legalDocumentId">The legal document ID.</param>
    /// <param name="asOfDate">The current date to compare against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next upcoming version if found; otherwise, null.</returns>
    Task<LegalDocumentVersion?> GetUpcomingVersionAsync(Guid legalDocumentId, DateTimeOffset asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new version.
    /// </summary>
    /// <param name="version">The version to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added version.</returns>
    Task<LegalDocumentVersion> AddAsync(LegalDocumentVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing version.
    /// </summary>
    /// <param name="version">The version to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(LegalDocumentVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a version number already exists for a document.
    /// </summary>
    /// <param name="legalDocumentId">The legal document ID.</param>
    /// <param name="versionNumber">The version number to check.</param>
    /// <param name="excludeVersionId">Optional version ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the version number exists; otherwise, false.</returns>
    Task<bool> VersionNumberExistsAsync(Guid legalDocumentId, string versionNumber, Guid? excludeVersionId = null, CancellationToken cancellationToken = default);
}
