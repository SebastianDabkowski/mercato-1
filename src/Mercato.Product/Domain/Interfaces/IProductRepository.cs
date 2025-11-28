using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Domain.Interfaces;

/// <summary>
/// Repository interface for product data access operations.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Entities.Product?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all products for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of products belonging to the store.</returns>
    Task<IReadOnlyList<Entities.Product>> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Adds a new product to the repository.
    /// </summary>
    /// <param name="product">The product to add.</param>
    /// <returns>The added product.</returns>
    Task<Entities.Product> AddAsync(Entities.Product product);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="product">The product to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Entities.Product product);

    /// <summary>
    /// Deletes a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Gets all active (non-archived) products for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of active products belonging to the store.</returns>
    Task<IReadOnlyList<Entities.Product>> GetActiveByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets multiple products by their unique identifiers.
    /// </summary>
    /// <param name="ids">The product IDs.</param>
    /// <returns>A list of products found.</returns>
    Task<IReadOnlyList<Entities.Product>> GetByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Updates multiple products in a single transaction.
    /// </summary>
    /// <param name="products">The products to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateManyAsync(IEnumerable<Entities.Product> products);

    /// <summary>
    /// Gets active products by category name with pagination support.
    /// Only products with Active status are returned.
    /// </summary>
    /// <param name="categoryName">The category name to filter by.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A tuple containing the list of products and total count.</returns>
    Task<(IReadOnlyList<Entities.Product> Products, int TotalCount)> GetActiveByCategoryAsync(string categoryName, int page, int pageSize);

    /// <summary>
    /// Searches for active products by keyword in title and description with pagination support.
    /// Only products with Active status are returned.
    /// </summary>
    /// <param name="searchQuery">The search query to match against title and description.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A tuple containing the list of matching products and total count.</returns>
    Task<(IReadOnlyList<Entities.Product> Products, int TotalCount)> SearchActiveProductsAsync(string searchQuery, int page, int pageSize);

    /// <summary>
    /// Searches for active products with filters applied.
    /// Only products with Active status are returned.
    /// </summary>
    /// <param name="searchQuery">Optional search query to match against title and description.</param>
    /// <param name="categoryName">Optional category name to filter by.</param>
    /// <param name="minPrice">Optional minimum price filter (inclusive).</param>
    /// <param name="maxPrice">Optional maximum price filter (inclusive).</param>
    /// <param name="condition">Optional condition filter ("InStock" or "OutOfStock").</param>
    /// <param name="storeId">Optional store ID to filter by seller.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A tuple containing the list of matching products and total count.</returns>
    Task<(IReadOnlyList<Entities.Product> Products, int TotalCount)> SearchActiveProductsWithFiltersAsync(
        string? searchQuery,
        string? categoryName,
        decimal? minPrice,
        decimal? maxPrice,
        string? condition,
        Guid? storeId,
        int page,
        int pageSize);

    /// <summary>
    /// Gets the price range (min and max) of all active products.
    /// </summary>
    /// <returns>A tuple containing the minimum and maximum prices, or null if no products exist.</returns>
    Task<(decimal? MinPrice, decimal? MaxPrice)> GetActivePriceRangeAsync();

    /// <summary>
    /// Gets all unique store IDs that have active products.
    /// </summary>
    /// <returns>A list of store IDs with active products.</returns>
    Task<IReadOnlyList<Guid>> GetActiveProductStoreIdsAsync();
}
