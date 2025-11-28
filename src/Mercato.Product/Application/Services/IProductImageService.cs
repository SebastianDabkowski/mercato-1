using Mercato.Product.Application.Commands;
using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Application.Services;

/// <summary>
/// Service interface for product image management operations.
/// </summary>
public interface IProductImageService
{
    /// <summary>
    /// Uploads a product image, generating thumbnail and optimized versions.
    /// </summary>
    /// <param name="command">The upload command containing file data and metadata.</param>
    /// <returns>The result of the upload operation.</returns>
    Task<UploadProductImageResult> UploadImageAsync(UploadProductImageCommand command);

    /// <summary>
    /// Deletes a product image and its optimized versions.
    /// </summary>
    /// <param name="command">The delete command.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<DeleteProductImageResult> DeleteImageAsync(DeleteProductImageCommand command);

    /// <summary>
    /// Sets a specific image as the main image for a product.
    /// </summary>
    /// <param name="command">The set main image command.</param>
    /// <returns>The result of the operation.</returns>
    Task<SetMainProductImageResult> SetMainImageAsync(SetMainProductImageCommand command);

    /// <summary>
    /// Gets all images for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <returns>A list of product images belonging to the product.</returns>
    Task<IReadOnlyList<ProductImage>> GetImagesByProductIdAsync(Guid productId);
}
