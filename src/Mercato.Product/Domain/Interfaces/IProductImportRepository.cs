using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Domain.Interfaces;

/// <summary>
/// Repository interface for product import job data access operations.
/// </summary>
public interface IProductImportRepository
{
    /// <summary>
    /// Adds a new product import job.
    /// </summary>
    /// <param name="job">The import job to add.</param>
    /// <returns>The added import job.</returns>
    Task<ProductImportJob> AddAsync(ProductImportJob job);

    /// <summary>
    /// Gets an import job by its unique identifier.
    /// </summary>
    /// <param name="id">The import job ID.</param>
    /// <returns>The import job if found; otherwise, null.</returns>
    Task<ProductImportJob?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all import jobs for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of import jobs belonging to the store.</returns>
    Task<IReadOnlyList<ProductImportJob>> GetByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Updates an existing import job.
    /// </summary>
    /// <param name="job">The import job to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ProductImportJob job);

    /// <summary>
    /// Adds row errors for an import job.
    /// </summary>
    /// <param name="errors">The row errors to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRowErrorsAsync(IEnumerable<ProductImportRowError> errors);

    /// <summary>
    /// Gets row errors for an import job.
    /// </summary>
    /// <param name="jobId">The import job ID.</param>
    /// <returns>A list of row errors for the job.</returns>
    Task<IReadOnlyList<ProductImportRowError>> GetRowErrorsByJobIdAsync(Guid jobId);

    /// <summary>
    /// Gets a product by SKU for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="sku">The product SKU.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Entities.Product?> GetProductBySkuAsync(Guid storeId, string sku);

    /// <summary>
    /// Gets products by SKUs for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="skus">The product SKUs.</param>
    /// <returns>A dictionary mapping SKU to product.</returns>
    Task<IDictionary<string, Entities.Product>> GetProductsBySkusAsync(Guid storeId, IEnumerable<string> skus);
}
