using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;

namespace Mercato.Buyer.Application.Services;

/// <summary>
/// Service interface for consent management operations.
/// </summary>
public interface IConsentService
{
    /// <summary>
    /// Gets all active consent types with their current versions.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>The result containing consent types.</returns>
    Task<GetConsentTypesResult> GetConsentTypesAsync(GetConsentTypesQuery query);

    /// <summary>
    /// Gets a user's current consents.
    /// </summary>
    /// <param name="query">The query containing the user ID.</param>
    /// <returns>The result containing user consents.</returns>
    Task<GetUserConsentsResult> GetUserConsentsAsync(GetUserConsentsQuery query);

    /// <summary>
    /// Records a single consent decision.
    /// </summary>
    /// <param name="command">The command containing consent details.</param>
    /// <returns>The result of the operation.</returns>
    Task<RecordConsentResult> RecordConsentAsync(RecordConsentCommand command);

    /// <summary>
    /// Records multiple consent decisions at once (e.g., during registration).
    /// </summary>
    /// <param name="command">The command containing multiple consent decisions.</param>
    /// <returns>The result of the operation.</returns>
    Task<RecordMultipleConsentsResult> RecordMultipleConsentsAsync(RecordMultipleConsentsCommand command);

    /// <summary>
    /// Checks if a user has an active consent for a specific type.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="consentTypeCode">The consent type code.</param>
    /// <returns>True if consent is active; otherwise, false.</returns>
    Task<bool> HasActiveConsentAsync(string userId, string consentTypeCode);
}
