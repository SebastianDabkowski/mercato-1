using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for legal document version management.
/// </summary>
public class LegalDocumentVersionRepository : ILegalDocumentVersionRepository
{
    private readonly AdminDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalDocumentVersionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public LegalDocumentVersionRepository(AdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocumentVersions
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LegalDocumentVersion>> GetByDocumentIdAsync(Guid legalDocumentId, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocumentVersions
            .Where(v => v.LegalDocumentId == legalDocumentId)
            .OrderByDescending(v => v.EffectiveDate)
            .ThenByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentVersion?> GetActiveVersionAsync(Guid legalDocumentId, DateTimeOffset asOfDate, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocumentVersions
            .Where(v => v.LegalDocumentId == legalDocumentId
                && v.IsPublished
                && v.EffectiveDate <= asOfDate)
            .OrderByDescending(v => v.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentVersion?> GetActiveVersionByTypeAsync(LegalDocumentType documentType, DateTimeOffset asOfDate, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocumentVersions
            .Join(_context.LegalDocuments,
                v => v.LegalDocumentId,
                d => d.Id,
                (v, d) => new { Version = v, Document = d })
            .Where(x => x.Document.DocumentType == documentType
                && x.Version.IsPublished
                && x.Version.EffectiveDate <= asOfDate)
            .OrderByDescending(x => x.Version.EffectiveDate)
            .Select(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentVersion?> GetUpcomingVersionAsync(Guid legalDocumentId, DateTimeOffset asOfDate, CancellationToken cancellationToken = default)
    {
        return await _context.LegalDocumentVersions
            .Where(v => v.LegalDocumentId == legalDocumentId
                && v.IsPublished
                && v.EffectiveDate > asOfDate)
            .OrderBy(v => v.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LegalDocumentVersion> AddAsync(LegalDocumentVersion version, CancellationToken cancellationToken = default)
    {
        _context.LegalDocumentVersions.Add(version);
        await _context.SaveChangesAsync(cancellationToken);
        return version;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(LegalDocumentVersion version, CancellationToken cancellationToken = default)
    {
        _context.LegalDocumentVersions.Update(version);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> VersionNumberExistsAsync(Guid legalDocumentId, string versionNumber, Guid? excludeVersionId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.LegalDocumentVersions
            .Where(v => v.LegalDocumentId == legalDocumentId && v.VersionNumber == versionNumber);

        if (excludeVersionId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVersionId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
