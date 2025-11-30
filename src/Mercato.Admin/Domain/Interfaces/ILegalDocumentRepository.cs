using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Domain.Interfaces;

/// <summary>
/// Repository interface for legal document management.
/// </summary>
public interface ILegalDocumentRepository
{
    /// <summary>
    /// Gets a legal document by its unique identifier.
    /// </summary>
    /// <param name="id">The legal document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The legal document if found; otherwise, null.</returns>
    Task<LegalDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a legal document by its type.
    /// </summary>
    /// <param name="documentType">The type of legal document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The legal document if found; otherwise, null.</returns>
    Task<LegalDocument?> GetByTypeAsync(LegalDocumentType documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all legal documents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all legal documents.</returns>
    Task<IReadOnlyList<LegalDocument>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new legal document.
    /// </summary>
    /// <param name="document">The legal document to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added legal document.</returns>
    Task<LegalDocument> AddAsync(LegalDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing legal document.
    /// </summary>
    /// <param name="document">The legal document to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(LegalDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a legal document exists for the given type.
    /// </summary>
    /// <param name="documentType">The type of legal document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a document exists for the type; otherwise, false.</returns>
    Task<bool> ExistsByTypeAsync(LegalDocumentType documentType, CancellationToken cancellationToken = default);
}
