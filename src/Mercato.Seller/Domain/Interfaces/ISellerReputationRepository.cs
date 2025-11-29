using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Domain.Interfaces;

/// <summary>
/// Repository interface for seller reputation operations.
/// </summary>
public interface ISellerReputationRepository
{
    /// <summary>
    /// Gets the reputation for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The seller reputation if found; otherwise, null.</returns>
    Task<SellerReputation?> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Creates a new seller reputation record.
    /// </summary>
    /// <param name="reputation">The reputation to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(SellerReputation reputation);

    /// <summary>
    /// Updates an existing seller reputation record.
    /// </summary>
    /// <param name="reputation">The reputation to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(SellerReputation reputation);

    /// <summary>
    /// Gets reputations for multiple stores by their IDs.
    /// </summary>
    /// <param name="storeIds">The store IDs to retrieve reputations for.</param>
    /// <returns>A list of seller reputations matching the provided store IDs.</returns>
    Task<IReadOnlyList<SellerReputation>> GetByStoreIdsAsync(IEnumerable<Guid> storeIds);
}
