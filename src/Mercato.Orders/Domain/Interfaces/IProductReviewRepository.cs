using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for product review data access operations.
/// </summary>
public interface IProductReviewRepository
{
    /// <summary>
    /// Gets a product review by its unique identifier.
    /// </summary>
    /// <param name="id">The product review ID.</param>
    /// <returns>The product review if found; otherwise, null.</returns>
    Task<ProductReview?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all product reviews by a specific buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A list of product reviews for the buyer.</returns>
    Task<IReadOnlyList<ProductReview>> GetByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Gets all product reviews for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of product reviews for the product.</returns>
    Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(Guid productId);

    /// <summary>
    /// Gets paginated product reviews for a specific product with sorting and average rating calculation.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of reviews per page.</param>
    /// <param name="sortBy">The sort option for reviews.</param>
    /// <returns>A tuple containing the reviews for the current page, total count of reviews, and average rating.</returns>
    Task<(IReadOnlyList<ProductReview> reviews, int totalCount, double? averageRating)> GetPagedByProductIdAsync(
        Guid productId,
        int page,
        int pageSize,
        ReviewSortOption sortBy);

    /// <summary>
    /// Gets all product reviews for a specific order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>A list of product reviews for the order.</returns>
    Task<IReadOnlyList<ProductReview>> GetByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Gets the product review for a specific seller sub-order item.
    /// </summary>
    /// <param name="itemId">The seller sub-order item ID.</param>
    /// <returns>The product review if found; otherwise, null.</returns>
    Task<ProductReview?> GetBySellerSubOrderItemIdAsync(Guid itemId);

    /// <summary>
    /// Adds a new product review to the repository.
    /// </summary>
    /// <param name="review">The product review to add.</param>
    /// <returns>The added product review.</returns>
    Task<ProductReview> AddAsync(ProductReview review);

    /// <summary>
    /// Updates an existing product review.
    /// </summary>
    /// <param name="review">The product review to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ProductReview review);

    /// <summary>
    /// Checks whether a review exists for a specific seller sub-order item by a buyer.
    /// </summary>
    /// <param name="sellerSubOrderItemId">The seller sub-order item ID.</param>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>True if a review exists; otherwise, false.</returns>
    Task<bool> ExistsForItemAsync(Guid sellerSubOrderItemId, string buyerId);

    /// <summary>
    /// Gets the timestamp of the buyer's last review submission for rate limiting.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The timestamp of the last review; null if no reviews exist.</returns>
    Task<DateTimeOffset?> GetBuyerLastReviewTimeAsync(string buyerId);

    /// <summary>
    /// Gets filtered and paginated product reviews for admin moderation.
    /// </summary>
    /// <param name="searchText">Optional search text to filter reviews by content.</param>
    /// <param name="statuses">Optional list of statuses to filter by.</param>
    /// <param name="fromDate">Optional start date for date range filter.</param>
    /// <param name="toDate">Optional end date for date range filter.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of reviews per page.</param>
    /// <returns>A tuple containing the reviews for the current page and total count.</returns>
    Task<(IReadOnlyList<ProductReview> reviews, int totalCount)> GetAllFilteredAsync(
        string? searchText,
        IReadOnlyList<ReviewStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize);

    /// <summary>
    /// Gets product reviews by status.
    /// </summary>
    /// <param name="status">The review status to filter by.</param>
    /// <returns>A list of product reviews with the specified status.</returns>
    Task<IReadOnlyList<ProductReview>> GetByStatusAsync(ReviewStatus status);
}
