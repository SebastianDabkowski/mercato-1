using Mercato.Product.Application.Commands;
using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Application.Services;

/// <summary>
/// Service interface for product management operations.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Creates a new product in the specified store's catalog.
    /// </summary>
    /// <param name="command">The create product command.</param>
    /// <returns>The result of the create operation.</returns>
    Task<CreateProductResult> CreateProductAsync(CreateProductCommand command);

    /// <summary>
    /// Gets a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The product if found; otherwise, null.</returns>
    Task<Domain.Entities.Product?> GetProductByIdAsync(Guid id);

    /// <summary>
    /// Gets all products for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of products belonging to the store.</returns>
    Task<IReadOnlyList<Domain.Entities.Product>> GetProductsByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="command">The update product command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateProductResult> UpdateProductAsync(UpdateProductCommand command);

    /// <summary>
    /// Archives (soft-deletes) a product.
    /// </summary>
    /// <param name="command">The archive product command.</param>
    /// <returns>The result of the archive operation.</returns>
    Task<ArchiveProductResult> ArchiveProductAsync(ArchiveProductCommand command);

    /// <summary>
    /// Gets all active (non-archived) products for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of active products belonging to the store.</returns>
    Task<IReadOnlyList<Domain.Entities.Product>> GetActiveProductsByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Changes the workflow status of a product.
    /// </summary>
    /// <param name="command">The change status command.</param>
    /// <returns>The result of the status change operation.</returns>
    Task<ChangeProductStatusResult> ChangeProductStatusAsync(ChangeProductStatusCommand command);

    /// <summary>
    /// Bulk updates price and/or stock for multiple products.
    /// </summary>
    /// <param name="command">The bulk update command.</param>
    /// <returns>The result of the bulk update operation.</returns>
    Task<BulkUpdatePriceStockResult> BulkUpdatePriceStockAsync(BulkUpdatePriceStockCommand command);

    /// <summary>
    /// Exports the product catalog to CSV or Excel format.
    /// </summary>
    /// <param name="command">The export command.</param>
    /// <returns>The result of the export operation containing the file content.</returns>
    Task<ExportProductCatalogResult> ExportProductCatalogAsync(ExportProductCatalogCommand command);

    /// <summary>
    /// Gets active products by category with pagination.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A tuple containing products and total count.</returns>
    Task<(IReadOnlyList<Domain.Entities.Product> Products, int TotalCount)> GetProductsByCategoryAsync(string categoryName, int page, int pageSize);

    /// <summary>
    /// Searches for active products by keyword with pagination.
    /// </summary>
    /// <param name="searchQuery">The search query to match against title and description.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A tuple containing matching products and total count.</returns>
    Task<(IReadOnlyList<Domain.Entities.Product> Products, int TotalCount)> SearchProductsAsync(string searchQuery, int page, int pageSize);
}
