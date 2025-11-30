using Mercato.Buyer.Domain.Entities;
using Mercato.Buyer.Domain.Interfaces;
using Mercato.Buyer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Buyer.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for consent data access operations.
/// </summary>
public class ConsentRepository : IConsentRepository
{
    private readonly BuyerDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentRepository"/> class.
    /// </summary>
    /// <param name="context">The buyer database context.</param>
    public ConsentRepository(BuyerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsentType>> GetActiveConsentTypesAsync()
    {
        return await _context.ConsentTypes
            .Where(ct => ct.IsActive)
            .Include(ct => ct.Versions.Where(v => v.EffectiveTo == null))
            .OrderBy(ct => ct.DisplayOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ConsentType?> GetConsentTypeByCodeAsync(string code)
    {
        return await _context.ConsentTypes
            .Include(ct => ct.Versions.Where(v => v.EffectiveTo == null))
            .FirstOrDefaultAsync(ct => ct.Code == code && ct.IsActive);
    }

    /// <inheritdoc />
    public async Task<ConsentVersion?> GetCurrentVersionAsync(Guid consentTypeId)
    {
        return await _context.ConsentVersions
            .Include(v => v.ConsentType)
            .Where(v => v.ConsentTypeId == consentTypeId && v.EffectiveTo == null)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<ConsentVersion?> GetVersionByIdAsync(Guid versionId)
    {
        return await _context.ConsentVersions
            .Include(v => v.ConsentType)
            .FirstOrDefaultAsync(v => v.Id == versionId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserConsent>> GetUserConsentsAsync(string userId)
    {
        return await _context.UserConsents
            .Include(uc => uc.ConsentVersion)
                .ThenInclude(v => v!.ConsentType)
            .Where(uc => uc.UserId == userId)
            .OrderByDescending(uc => uc.ConsentedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UserConsent?> GetLatestUserConsentAsync(string userId, Guid consentTypeId)
    {
        return await _context.UserConsents
            .Include(uc => uc.ConsentVersion)
                .ThenInclude(v => v!.ConsentType)
            .Where(uc => uc.UserId == userId && uc.ConsentVersion!.ConsentTypeId == consentTypeId)
            .OrderByDescending(uc => uc.ConsentedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserConsent>> GetUserConsentHistoryAsync(string userId, Guid consentTypeId)
    {
        return await _context.UserConsents
            .Include(uc => uc.ConsentVersion)
            .Where(uc => uc.UserId == userId && uc.ConsentVersion!.ConsentTypeId == consentTypeId)
            .OrderByDescending(uc => uc.ConsentedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveConsentAsync(string userId, string consentTypeCode)
    {
        var consentType = await _context.ConsentTypes
            .FirstOrDefaultAsync(ct => ct.Code == consentTypeCode && ct.IsActive);

        if (consentType == null)
        {
            return false;
        }

        var latestConsent = await _context.UserConsents
            .Where(uc => uc.UserId == userId && uc.ConsentVersion!.ConsentTypeId == consentType.Id)
            .OrderByDescending(uc => uc.ConsentedAt)
            .FirstOrDefaultAsync();

        return latestConsent?.IsGranted ?? false;
    }

    /// <inheritdoc />
    public async Task<UserConsent> AddUserConsentAsync(UserConsent consent)
    {
        _context.UserConsents.Add(consent);
        await _context.SaveChangesAsync();
        return consent;
    }

    /// <inheritdoc />
    public async Task<ConsentType> AddConsentTypeAsync(ConsentType consentType)
    {
        _context.ConsentTypes.Add(consentType);
        await _context.SaveChangesAsync();
        return consentType;
    }

    /// <inheritdoc />
    public async Task<ConsentVersion> AddConsentVersionAsync(ConsentVersion version)
    {
        _context.ConsentVersions.Add(version);
        await _context.SaveChangesAsync();
        return version;
    }

    /// <inheritdoc />
    public async Task UpdateConsentTypeAsync(ConsentType consentType)
    {
        _context.ConsentTypes.Update(consentType);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateConsentVersionAsync(ConsentVersion version)
    {
        _context.ConsentVersions.Update(version);
        await _context.SaveChangesAsync();
    }
}
