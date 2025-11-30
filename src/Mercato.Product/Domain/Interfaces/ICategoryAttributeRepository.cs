using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Domain.Interfaces;

/// <summary>
/// Repository interface for category attribute data access operations.
/// </summary>
public interface ICategoryAttributeRepository
{
    /// <summary>
    /// Gets a category attribute by its unique identifier.
    /// </summary>
    /// <param name="id">The attribute ID.</param>
    /// <returns>The attribute if found; otherwise, null.</returns>
    Task<CategoryAttribute?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all attributes for a specific category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>A list of attributes for the category.</returns>
    Task<IReadOnlyList<CategoryAttribute>> GetByCategoryIdAsync(Guid categoryId);

    /// <summary>
    /// Gets all non-deprecated attributes for a specific category.
    /// Used when sellers create new products.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>A list of active (non-deprecated) attributes for the category.</returns>
    Task<IReadOnlyList<CategoryAttribute>> GetActiveByCategoryIdAsync(Guid categoryId);

    /// <summary>
    /// Gets attributes for multiple categories.
    /// Useful for fetching attributes for a category and its ancestors.
    /// </summary>
    /// <param name="categoryIds">The category IDs.</param>
    /// <returns>A list of attributes for all specified categories.</returns>
    Task<IReadOnlyList<CategoryAttribute>> GetByCategoryIdsAsync(IEnumerable<Guid> categoryIds);

    /// <summary>
    /// Adds a new category attribute.
    /// </summary>
    /// <param name="attribute">The attribute to add.</param>
    /// <returns>The added attribute.</returns>
    Task<CategoryAttribute> AddAsync(CategoryAttribute attribute);

    /// <summary>
    /// Updates an existing category attribute.
    /// </summary>
    /// <param name="attribute">The attribute to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(CategoryAttribute attribute);

    /// <summary>
    /// Deletes a category attribute by its unique identifier.
    /// </summary>
    /// <param name="id">The attribute ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Checks if an attribute with the specified name exists within the category.
    /// </summary>
    /// <param name="name">The attribute name to check.</param>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="excludeAttributeId">Optional attribute ID to exclude from the check (for updates).</param>
    /// <returns>True if an attribute with the name exists; otherwise, false.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid categoryId, Guid? excludeAttributeId = null);
}
