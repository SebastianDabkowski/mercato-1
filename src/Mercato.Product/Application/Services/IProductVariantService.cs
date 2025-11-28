using Mercato.Product.Application.Commands;
using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Application.Services;

/// <summary>
/// Service interface for product variant management operations.
/// </summary>
public interface IProductVariantService
{
    /// <summary>
    /// Configures variants for a product, replacing any existing variant configuration.
    /// </summary>
    /// <param name="command">The configure variants command.</param>
    /// <returns>The result of the configure operation.</returns>
    Task<ConfigureProductVariantsResult> ConfigureVariantsAsync(ConfigureProductVariantsCommand command);

    /// <summary>
    /// Updates an existing product variant.
    /// </summary>
    /// <param name="command">The update variant command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateProductVariantResult> UpdateVariantAsync(UpdateProductVariantCommand command);

    /// <summary>
    /// Gets all variants for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of variants belonging to the product.</returns>
    Task<IReadOnlyList<ProductVariant>> GetVariantsByProductIdAsync(Guid productId);

    /// <summary>
    /// Gets all active variants for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of active variants belonging to the product.</returns>
    Task<IReadOnlyList<ProductVariant>> GetActiveVariantsByProductIdAsync(Guid productId);

    /// <summary>
    /// Gets a variant by its unique identifier.
    /// </summary>
    /// <param name="id">The variant ID.</param>
    /// <returns>The variant if found; otherwise, null.</returns>
    Task<ProductVariant?> GetVariantByIdAsync(Guid id);

    /// <summary>
    /// Gets all variant attributes for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of variant attributes with their values.</returns>
    Task<IReadOnlyList<ProductVariantAttribute>> GetAttributesByProductIdAsync(Guid productId);

    /// <summary>
    /// Removes all variants and variant attributes from a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for authorization.</param>
    /// <param name="sellerId">The seller ID performing this action.</param>
    /// <returns>The result of the remove operation.</returns>
    Task<ConfigureProductVariantsResult> RemoveVariantsAsync(Guid productId, Guid storeId, string sellerId);
}
