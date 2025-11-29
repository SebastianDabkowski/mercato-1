using Mercato.Orders.Application.Commands;

namespace Mercato.Orders.Application.Services;

/// <summary>
/// Service interface for seller rating management operations.
/// </summary>
public interface ISellerRatingService
{
    /// <summary>
    /// Submits a new seller rating for a delivered sub-order.
    /// </summary>
    /// <param name="command">The submit rating command.</param>
    /// <returns>The result of the submit operation.</returns>
    Task<SubmitSellerRatingResult> SubmitRatingAsync(SubmitSellerRatingCommand command);

    /// <summary>
    /// Gets all seller ratings for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The result containing the store's ratings.</returns>
    Task<GetSellerRatingsResult> GetRatingsByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets all seller ratings submitted by a specific buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The result containing the buyer's ratings.</returns>
    Task<GetSellerRatingsResult> GetRatingsByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Checks whether a rating can be submitted for a specific sub-order.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order ID.</param>
    /// <param name="buyerId">The buyer ID for authorization.</param>
    /// <returns>The result indicating whether a rating can be submitted.</returns>
    Task<CanSubmitSellerRatingResult> CanSubmitRatingAsync(Guid sellerSubOrderId, string buyerId);

    /// <summary>
    /// Gets the average rating for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The result containing the average rating.</returns>
    Task<GetAverageRatingResult> GetAverageRatingForStoreAsync(Guid storeId);
}
