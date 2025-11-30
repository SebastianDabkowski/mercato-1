using Mercato.Buyer.Domain.Entities;

namespace Mercato.Buyer.Domain.Interfaces;

/// <summary>
/// Repository interface for consent data access operations.
/// </summary>
public interface IConsentRepository
{
    /// <summary>
    /// Gets all active consent types.
    /// </summary>
    /// <returns>A read-only list of active consent types with their current versions.</returns>
    Task<IReadOnlyList<ConsentType>> GetActiveConsentTypesAsync();

    /// <summary>
    /// Gets a consent type by its code.
    /// </summary>
    /// <param name="code">The consent type code.</param>
    /// <returns>The consent type if found; otherwise, null.</returns>
    Task<ConsentType?> GetConsentTypeByCodeAsync(string code);

    /// <summary>
    /// Gets the current (latest effective) version for a consent type.
    /// </summary>
    /// <param name="consentTypeId">The consent type identifier.</param>
    /// <returns>The current consent version if found; otherwise, null.</returns>
    Task<ConsentVersion?> GetCurrentVersionAsync(Guid consentTypeId);

    /// <summary>
    /// Gets a consent version by its identifier.
    /// </summary>
    /// <param name="versionId">The consent version identifier.</param>
    /// <returns>The consent version if found; otherwise, null.</returns>
    Task<ConsentVersion?> GetVersionByIdAsync(Guid versionId);

    /// <summary>
    /// Gets all user consents for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A read-only list of user consents with their versions and types.</returns>
    Task<IReadOnlyList<UserConsent>> GetUserConsentsAsync(string userId);

    /// <summary>
    /// Gets the latest consent record for a user and consent type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentTypeId">The consent type identifier.</param>
    /// <returns>The latest user consent if found; otherwise, null.</returns>
    Task<UserConsent?> GetLatestUserConsentAsync(string userId, Guid consentTypeId);

    /// <summary>
    /// Gets all consent history for a user and consent type (for audit purposes).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentTypeId">The consent type identifier.</param>
    /// <returns>A read-only list of all consent records, ordered by date descending.</returns>
    Task<IReadOnlyList<UserConsent>> GetUserConsentHistoryAsync(string userId, Guid consentTypeId);

    /// <summary>
    /// Checks if a user has an active (granted) consent for a specific type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentTypeCode">The consent type code.</param>
    /// <returns>True if consent is active; otherwise, false.</returns>
    Task<bool> HasActiveConsentAsync(string userId, string consentTypeCode);

    /// <summary>
    /// Adds a new user consent record.
    /// </summary>
    /// <param name="consent">The user consent to add.</param>
    /// <returns>The added user consent.</returns>
    Task<UserConsent> AddUserConsentAsync(UserConsent consent);

    /// <summary>
    /// Adds a new consent type.
    /// </summary>
    /// <param name="consentType">The consent type to add.</param>
    /// <returns>The added consent type.</returns>
    Task<ConsentType> AddConsentTypeAsync(ConsentType consentType);

    /// <summary>
    /// Adds a new consent version.
    /// </summary>
    /// <param name="version">The consent version to add.</param>
    /// <returns>The added consent version.</returns>
    Task<ConsentVersion> AddConsentVersionAsync(ConsentVersion version);

    /// <summary>
    /// Updates an existing consent type.
    /// </summary>
    /// <param name="consentType">The consent type to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateConsentTypeAsync(ConsentType consentType);

    /// <summary>
    /// Updates an existing consent version.
    /// </summary>
    /// <param name="version">The consent version to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateConsentVersionAsync(ConsentVersion version);
}
