using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Admin.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for legal consent tracking.
/// </summary>
public class LegalConsentRepository : ILegalConsentRepository
{
    private readonly AdminDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegalConsentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public LegalConsentRepository(AdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<LegalConsent> AddAsync(LegalConsent consent, CancellationToken cancellationToken = default)
    {
        _context.LegalConsents.Add(consent);
        await _context.SaveChangesAsync(cancellationToken);
        return consent;
    }

    /// <inheritdoc/>
    public async Task<LegalConsent?> GetLatestConsentAsync(string userId, LegalDocumentType documentType, CancellationToken cancellationToken = default)
    {
        return await _context.LegalConsents
            .Where(c => c.UserId == userId && c.DocumentType == documentType)
            .OrderByDescending(c => c.ConsentedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LegalConsent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.LegalConsents
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ConsentedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LegalConsent>> GetByVersionIdAsync(Guid legalDocumentVersionId, CancellationToken cancellationToken = default)
    {
        return await _context.LegalConsents
            .Where(c => c.LegalDocumentVersionId == legalDocumentVersionId)
            .OrderByDescending(c => c.ConsentedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasConsentedAsync(string userId, Guid legalDocumentVersionId, CancellationToken cancellationToken = default)
    {
        return await _context.LegalConsents
            .AnyAsync(c => c.UserId == userId && c.LegalDocumentVersionId == legalDocumentVersionId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasConsentedToCurrentVersionAsync(string userId, LegalDocumentType documentType, Guid currentVersionId, CancellationToken cancellationToken = default)
    {
        return await _context.LegalConsents
            .AnyAsync(c => c.UserId == userId 
                && c.DocumentType == documentType 
                && c.LegalDocumentVersionId == currentVersionId, cancellationToken);
    }
}
