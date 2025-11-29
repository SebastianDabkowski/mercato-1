using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for seller rating data access operations.
/// </summary>
public interface ISellerRatingRepository
{
    /// <summary>
    /// Gets a seller rating by its unique identifier.
    /// </summary>
    /// <param name="id">The seller rating ID.</param>
    /// <returns>The seller rating if found; otherwise, null.</returns>
    Task<SellerRating?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all seller ratings submitted by a specific buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A list of seller ratings for the buyer.</returns>
    Task<IReadOnlyList<SellerRating>> GetByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Gets all seller ratings for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of seller ratings for the store.</returns>
    Task<IReadOnlyList<SellerRating>> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Adds a new seller rating to the repository.
    /// </summary>
    /// <param name="rating">The seller rating to add.</param>
    /// <returns>The added seller rating.</returns>
    Task<SellerRating> AddAsync(SellerRating rating);

    /// <summary>
    /// Updates an existing seller rating.
    /// </summary>
    /// <param name="rating">The seller rating to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(SellerRating rating);

    /// <summary>
    /// Checks whether a rating exists for a specific seller sub-order by a buyer.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order ID.</param>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>True if a rating exists; otherwise, false.</returns>
    Task<bool> ExistsForSubOrderAsync(Guid sellerSubOrderId, string buyerId);

    /// <summary>
    /// Gets the timestamp of the buyer's last rating submission for rate limiting.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The timestamp of the last rating; null if no ratings exist.</returns>
    Task<DateTimeOffset?> GetBuyerLastRatingTimeAsync(string buyerId);

    /// <summary>
    /// Gets the average rating for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The average rating; null if no ratings exist.</returns>
    Task<double?> GetAverageRatingForStoreAsync(Guid storeId);
}
