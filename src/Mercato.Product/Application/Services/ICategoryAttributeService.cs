using Mercato.Product.Application.Commands;
using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Application.Services;

/// <summary>
/// Service interface for category attribute management operations.
/// </summary>
public interface ICategoryAttributeService
{
    /// <summary>
    /// Creates a new category attribute.
    /// </summary>
    /// <param name="command">The create attribute command.</param>
    /// <returns>The result of the create operation.</returns>
    Task<CreateCategoryAttributeResult> CreateAttributeAsync(CreateCategoryAttributeCommand command);

    /// <summary>
    /// Gets all attributes for a specific category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>A list of attributes for the category.</returns>
    Task<IReadOnlyList<CategoryAttribute>> GetAttributesByCategoryIdAsync(Guid categoryId);

    /// <summary>
    /// Gets all non-deprecated attributes for a specific category.
    /// Used when sellers create new products.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>A list of active (non-deprecated) attributes for the category.</returns>
    Task<IReadOnlyList<CategoryAttribute>> GetActiveAttributesByCategoryIdAsync(Guid categoryId);

    /// <summary>
    /// Gets a category attribute by its unique identifier.
    /// </summary>
    /// <param name="id">The attribute ID.</param>
    /// <returns>The attribute if found; otherwise, null.</returns>
    Task<CategoryAttribute?> GetAttributeByIdAsync(Guid id);

    /// <summary>
    /// Updates an existing category attribute.
    /// </summary>
    /// <param name="command">The update attribute command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateCategoryAttributeResult> UpdateAttributeAsync(UpdateCategoryAttributeCommand command);

    /// <summary>
    /// Deletes a category attribute.
    /// </summary>
    /// <param name="command">The delete attribute command.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<DeleteCategoryAttributeResult> DeleteAttributeAsync(DeleteCategoryAttributeCommand command);

    /// <summary>
    /// Deprecates a category attribute.
    /// Deprecated attributes are hidden from new product creation but remain visible for existing products.
    /// </summary>
    /// <param name="attributeId">The attribute ID to deprecate.</param>
    /// <returns>The result of the deprecate operation.</returns>
    Task<UpdateCategoryAttributeResult> DeprecateAttributeAsync(Guid attributeId);

    /// <summary>
    /// Restores a deprecated category attribute.
    /// </summary>
    /// <param name="attributeId">The attribute ID to restore.</param>
    /// <returns>The result of the restore operation.</returns>
    Task<UpdateCategoryAttributeResult> RestoreAttributeAsync(Guid attributeId);
}
