using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Domain.Interfaces;

/// <summary>
/// Repository interface for product variant data access operations.
/// </summary>
public interface IProductVariantRepository
{
    /// <summary>
    /// Gets a product variant by its unique identifier.
    /// </summary>
    /// <param name="id">The variant ID.</param>
    /// <returns>The variant if found; otherwise, null.</returns>
    Task<ProductVariant?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all variants for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of variants belonging to the product.</returns>
    Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId);

    /// <summary>
    /// Gets all active variants for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of active variants belonging to the product.</returns>
    Task<IReadOnlyList<ProductVariant>> GetActiveByProductIdAsync(Guid productId);

    /// <summary>
    /// Gets a variant by its SKU within a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="sku">The variant SKU.</param>
    /// <returns>The variant if found; otherwise, null.</returns>
    Task<ProductVariant?> GetBySkuAsync(Guid storeId, string sku);

    /// <summary>
    /// Adds a new variant to the repository.
    /// </summary>
    /// <param name="variant">The variant to add.</param>
    /// <returns>The added variant.</returns>
    Task<ProductVariant> AddAsync(ProductVariant variant);

    /// <summary>
    /// Updates an existing variant.
    /// </summary>
    /// <param name="variant">The variant to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ProductVariant variant);

    /// <summary>
    /// Deletes a variant by its unique identifier.
    /// </summary>
    /// <param name="id">The variant ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Gets all variant attributes for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of variant attributes belonging to the product.</returns>
    Task<IReadOnlyList<ProductVariantAttribute>> GetAttributesByProductIdAsync(Guid productId);

    /// <summary>
    /// Adds a new variant attribute to the repository.
    /// </summary>
    /// <param name="attribute">The attribute to add.</param>
    /// <returns>The added attribute.</returns>
    Task<ProductVariantAttribute> AddAttributeAsync(ProductVariantAttribute attribute);

    /// <summary>
    /// Deletes all variant attributes and values for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAttributesByProductIdAsync(Guid productId);

    /// <summary>
    /// Deletes all variants for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteVariantsByProductIdAsync(Guid productId);

    /// <summary>
    /// Adds multiple variants in a single transaction.
    /// </summary>
    /// <param name="variants">The variants to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddManyAsync(IEnumerable<ProductVariant> variants);

    /// <summary>
    /// Updates multiple variants in a single transaction.
    /// </summary>
    /// <param name="variants">The variants to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateManyAsync(IEnumerable<ProductVariant> variants);
}
