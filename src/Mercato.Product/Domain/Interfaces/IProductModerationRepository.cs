using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Domain.Interfaces;

/// <summary>
/// Repository interface for product moderation operations.
/// </summary>
public interface IProductModerationRepository
{
    /// <summary>
    /// Gets products awaiting moderation with filtering and pagination.
    /// </summary>
    /// <param name="moderationStatuses">Optional list of moderation statuses to filter by.</param>
    /// <param name="category">Optional category to filter by.</param>
    /// <param name="searchTerm">Optional search term to filter by title or description.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A tuple containing the list of products and total count.</returns>
    Task<(IReadOnlyList<Entities.Product> Products, int TotalCount)> GetProductsForModerationAsync(
        IReadOnlyList<ProductModerationStatus>? moderationStatuses,
        string? category,
        string? searchTerm,
        int page,
        int pageSize);

    /// <summary>
    /// Gets a product by ID with moderation-related details.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Entities.Product?> GetProductForModerationAsync(Guid productId);

    /// <summary>
    /// Updates the moderation status of a product.
    /// </summary>
    /// <param name="product">The product to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateModerationStatusAsync(Entities.Product product);

    /// <summary>
    /// Updates the moderation status of multiple products.
    /// </summary>
    /// <param name="products">The products to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateModerationStatusBulkAsync(IEnumerable<Entities.Product> products);

    /// <summary>
    /// Adds a moderation decision record for audit history.
    /// </summary>
    /// <param name="decision">The moderation decision to add.</param>
    /// <returns>The added decision.</returns>
    Task<ProductModerationDecision> AddModerationDecisionAsync(ProductModerationDecision decision);

    /// <summary>
    /// Gets the moderation history for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of moderation decisions ordered by date descending.</returns>
    Task<IReadOnlyList<ProductModerationDecision>> GetModerationHistoryAsync(Guid productId);

    /// <summary>
    /// Gets all distinct categories from products.
    /// </summary>
    /// <returns>A list of distinct category names.</returns>
    Task<IReadOnlyList<string>> GetDistinctCategoriesAsync();

    /// <summary>
    /// Gets multiple products by their IDs.
    /// </summary>
    /// <param name="productIds">The product IDs.</param>
    /// <returns>A list of products found.</returns>
    Task<IReadOnlyList<Entities.Product>> GetProductsByIdsAsync(IEnumerable<Guid> productIds);
}
