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
}
