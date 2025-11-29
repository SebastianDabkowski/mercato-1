using Mercato.Orders.Application.Commands;

namespace Mercato.Orders.Application.Services;

/// <summary>
/// Service interface for product review management operations.
/// </summary>
public interface IProductReviewService
{
    /// <summary>
    /// Submits a new product review for a delivered order item.
    /// </summary>
    /// <param name="command">The submit review command.</param>
    /// <returns>The result of the submit operation.</returns>
    Task<SubmitProductReviewResult> SubmitReviewAsync(SubmitProductReviewCommand command);

    /// <summary>
    /// Gets all product reviews for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The result containing the product's reviews.</returns>
    Task<GetProductReviewsResult> GetReviewsByProductIdAsync(Guid productId);

    /// <summary>
    /// Gets paginated product reviews for a specific product with sorting and average rating.
    /// </summary>
    /// <param name="query">The query with pagination and sorting options.</param>
    /// <returns>The result containing paginated reviews and metadata.</returns>
    Task<GetProductReviewsPagedResult> GetReviewsByProductIdPagedAsync(GetProductReviewsQuery query);

    /// <summary>
    /// Gets all product reviews submitted by a specific buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The result containing the buyer's reviews.</returns>
    Task<GetProductReviewsResult> GetReviewsByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Checks whether a review can be submitted for a specific item.
    /// </summary>
    /// <param name="sellerSubOrderItemId">The seller sub-order item ID.</param>
    /// <param name="buyerId">The buyer ID for authorization.</param>
    /// <returns>The result indicating whether a review can be submitted.</returns>
    Task<CanSubmitReviewResult> CanSubmitReviewAsync(Guid sellerSubOrderItemId, string buyerId);
}
