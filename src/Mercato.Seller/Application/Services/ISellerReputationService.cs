using Mercato.Seller.Application.Commands;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Services;

/// <summary>
/// Service interface for seller reputation operations.
/// </summary>
public interface ISellerReputationService
{
    /// <summary>
    /// Gets the reputation for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The seller reputation if found; otherwise, null.</returns>
    Task<SellerReputation?> GetReputationByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Calculates and updates the reputation for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="command">The command containing metrics for calculation.</param>
    /// <returns>The result of the calculation operation.</returns>
    Task<CalculateReputationResult> CalculateAndUpdateReputationAsync(Guid storeId, CalculateReputationCommand command);

    /// <summary>
    /// Gets reputations for multiple stores by their IDs.
    /// </summary>
    /// <param name="storeIds">The store IDs to retrieve reputations for.</param>
    /// <returns>A list of seller reputations matching the provided store IDs.</returns>
    Task<IReadOnlyList<SellerReputation>> GetReputationsByStoreIdsAsync(IEnumerable<Guid> storeIds);
}
