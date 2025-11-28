using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Domain.Interfaces;

/// <summary>
/// Repository interface for product image data access operations.
/// </summary>
public interface IProductImageRepository
{
    /// <summary>
    /// Adds a new product image to the repository.
    /// </summary>
    /// <param name="image">The product image to add.</param>
    /// <returns>The added product image.</returns>
    Task<ProductImage> AddAsync(ProductImage image);

    /// <summary>
    /// Gets all images for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of product images belonging to the product.</returns>
    Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(Guid productId);

    /// <summary>
    /// Gets a product image by its unique identifier.
    /// </summary>
    /// <param name="id">The image ID.</param>
    /// <returns>The product image if found; otherwise, null.</returns>
    Task<ProductImage?> GetByIdAsync(Guid id);

    /// <summary>
    /// Updates an existing product image.
    /// </summary>
    /// <param name="image">The product image to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ProductImage image);

    /// <summary>
    /// Deletes a product image by its unique identifier.
    /// </summary>
    /// <param name="id">The image ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Sets a specific image as the main image for a product.
    /// Clears the main flag from all other images of the same product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="imageId">The image ID to set as main.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetMainImageAsync(Guid productId, Guid imageId);

    /// <summary>
    /// Gets the count of images for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>The number of images for the product.</returns>
    Task<int> GetImageCountByProductIdAsync(Guid productId);
}
