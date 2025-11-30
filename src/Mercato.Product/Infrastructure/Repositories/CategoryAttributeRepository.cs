using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for category attribute data access operations.
/// </summary>
public class CategoryAttributeRepository : ICategoryAttributeRepository
{
    private readonly ProductDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryAttributeRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CategoryAttributeRepository(ProductDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CategoryAttribute?> GetByIdAsync(Guid id)
    {
        return await _context.CategoryAttributes.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryAttribute>> GetByCategoryIdAsync(Guid categoryId)
    {
        return await _context.CategoryAttributes
            .Where(a => a.CategoryId == categoryId)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryAttribute>> GetActiveByCategoryIdAsync(Guid categoryId)
    {
        return await _context.CategoryAttributes
            .Where(a => a.CategoryId == categoryId && !a.IsDeprecated)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryAttribute>> GetByCategoryIdsAsync(IEnumerable<Guid> categoryIds)
    {
        var categoryIdList = categoryIds.ToList();
        return await _context.CategoryAttributes
            .Where(a => categoryIdList.Contains(a.CategoryId))
            .OrderBy(a => a.CategoryId)
            .ThenBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CategoryAttribute> AddAsync(CategoryAttribute attribute)
    {
        _context.CategoryAttributes.Add(attribute);
        await _context.SaveChangesAsync();
        return attribute;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(CategoryAttribute attribute)
    {
        _context.CategoryAttributes.Update(attribute);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var attribute = await _context.CategoryAttributes.FindAsync(id);
        if (attribute != null)
        {
            _context.CategoryAttributes.Remove(attribute);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid categoryId, Guid? excludeAttributeId = null)
    {
        var query = _context.CategoryAttributes
            .Where(a => a.Name == name && a.CategoryId == categoryId);

        if (excludeAttributeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAttributeId.Value);
        }

        return await query.AnyAsync();
    }
}
