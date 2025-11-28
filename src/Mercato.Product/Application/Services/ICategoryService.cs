using Mercato.Product.Application.Commands;
using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Application.Services;

/// <summary>
/// Service interface for category management operations.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="command">The create category command.</param>
    /// <returns>The result of the create operation.</returns>
    Task<CreateCategoryResult> CreateCategoryAsync(CreateCategoryCommand command);

    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <returns>A list of all categories.</returns>
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync();

    /// <summary>
    /// Gets a category by its unique identifier.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <returns>The category if found; otherwise, null.</returns>
    Task<Category?> GetCategoryByIdAsync(Guid id);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="command">The update category command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateCategoryResult> UpdateCategoryAsync(UpdateCategoryCommand command);

    /// <summary>
    /// Deletes a category.
    /// </summary>
    /// <param name="command">The delete category command.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<DeleteCategoryResult> DeleteCategoryAsync(DeleteCategoryCommand command);

    /// <summary>
    /// Gets categories by parent ID.
    /// </summary>
    /// <param name="parentId">The parent category ID. Use null to get root categories.</param>
    /// <returns>A list of child categories.</returns>
    Task<IReadOnlyList<Category>> GetCategoriesByParentIdAsync(Guid? parentId);

    /// <summary>
    /// Gets active categories by parent ID.
    /// </summary>
    /// <param name="parentId">The parent category ID. Use null to get root categories.</param>
    /// <returns>A list of active child categories.</returns>
    Task<IReadOnlyList<Category>> GetActiveCategoriesByParentIdAsync(Guid? parentId);
}
