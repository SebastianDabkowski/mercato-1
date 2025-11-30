using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for legal document management.
/// </summary>
public class LegalDocumentRepository : ILegalDocumentRepository
{
    private readonly AdminDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalDocumentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public LegalDocumentRepository(AdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<LegalDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocuments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LegalDocument?> GetByTypeAsync(LegalDocumentType documentType, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocuments
            .FirstOrDefaultAsync(d => d.DocumentType == documentType, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LegalDocument>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocuments
            .OrderBy(d => d.DocumentType)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LegalDocument> AddAsync(LegalDocument document, CancellationToken cancellationToken = default)
    {
        _context.LegalDocuments.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(LegalDocument document, CancellationToken cancellationToken = default)
    {
        _context.LegalDocuments.Update(document);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByTypeAsync(LegalDocumentType documentType, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocuments
            .AnyAsync(d => d.DocumentType == documentType, cancellationToken);
    }
}
