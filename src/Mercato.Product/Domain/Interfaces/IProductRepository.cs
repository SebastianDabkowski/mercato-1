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
}
