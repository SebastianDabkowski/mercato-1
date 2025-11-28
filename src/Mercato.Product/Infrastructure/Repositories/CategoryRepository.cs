using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for category data access operations.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly ProductDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CategoryRepository(ProductDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.ParentId)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetByParentIdAsync(Guid? parentId)
    {
        return await _context.Categories
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Category> AddAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid? parentId, Guid? excludeCategoryId = null)
    {
        var query = _context.Categories
            .Where(c => c.Name == name && c.ParentId == parentId);

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCategoryId.Value);
        }

        return await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetProductCountAsync(Guid categoryId)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null)
        {
            return 0;
        }

        // Note: Product-Category relationship uses string matching (Product.Category = Category.Name)
        // rather than a foreign key. This design maintains backward compatibility with the existing
        // Product entity that stores category as a string. A future enhancement could add a CategoryId
        // foreign key to the Product entity for stronger data integrity.
        return await _context.Products
            .CountAsync(p => p.Category == category.Name);
    }

    /// <inheritdoc />
    public async Task<int> GetChildCountAsync(Guid parentId)
    {
        return await _context.Categories
            .CountAsync(c => c.ParentId == parentId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetActiveByParentIdAsync(Guid? parentId)
    {
        return await _context.Categories
            .Where(c => c.ParentId == parentId && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> SearchCategoriesAsync(string searchTerm, int maxResults)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        var likePattern = $"%{searchTerm}%";

        return await _context.Categories
            .Where(c => c.IsActive && EF.Functions.Like(c.Name, likePattern))
            .OrderBy(c => c.Name)
            .Take(maxResults)
            .ToListAsync();
    }
}
