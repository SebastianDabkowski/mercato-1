using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Domain.Interfaces;

/// <summary>
/// Repository interface for category data access operations.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets a category by its unique identifier.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <returns>The category if found; otherwise, null.</returns>
    Task<Category?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <returns>A list of all categories.</returns>
    Task<IReadOnlyList<Category>> GetAllAsync();

    /// <summary>
    /// Gets all categories by parent ID.
    /// </summary>
    /// <param name="parentId">The parent category ID. Use null to get root categories.</param>
    /// <returns>A list of child categories.</returns>
    Task<IReadOnlyList<Category>> GetByParentIdAsync(Guid? parentId);

    /// <summary>
    /// Adds a new category to the repository.
    /// </summary>
    /// <param name="category">The category to add.</param>
    /// <returns>The added category.</returns>
    Task<Category> AddAsync(Category category);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="category">The category to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Category category);

    /// <summary>
    /// Deletes a category by its unique identifier.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Checks if a category with the specified name exists within the same parent.
    /// </summary>
    /// <param name="name">The category name to check.</param>
    /// <param name="parentId">The parent category ID.</param>
    /// <param name="excludeCategoryId">Optional category ID to exclude from the check (for updates).</param>
    /// <returns>True if a category with the name exists; otherwise, false.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? parentId, Guid? excludeCategoryId = null);

    /// <summary>
    /// Gets the count of products assigned to a category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>The number of products assigned to the category.</returns>
    Task<int> GetProductCountAsync(Guid categoryId);

    /// <summary>
    /// Gets the count of child categories for a given parent.
    /// </summary>
    /// <param name="parentId">The parent category ID.</param>
    /// <returns>The number of child categories.</returns>
    Task<int> GetChildCountAsync(Guid parentId);
}
