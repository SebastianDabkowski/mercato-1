using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for product variant data access operations.
/// </summary>
public class ProductVariantRepository : IProductVariantRepository
{
    private readonly ProductDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductVariantRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ProductVariantRepository(ProductDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ProductVariant?> GetByIdAsync(Guid id)
    {
        return await _context.ProductVariants.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId)
    {
        return await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.AttributeCombination)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductVariant>> GetActiveByProductIdAsync(Guid productId)
    {
        return await _context.ProductVariants
            .Where(v => v.ProductId == productId && v.IsActive)
            .OrderBy(v => v.AttributeCombination)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductVariant?> GetBySkuAsync(Guid storeId, string sku)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product != null && v.Product.StoreId == storeId && v.Sku == sku)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<ProductVariant> AddAsync(ProductVariant variant)
    {
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        return variant;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var variant = await _context.ProductVariants.FindAsync(id);
        if (variant != null)
        {
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductVariantAttribute>> GetAttributesByProductIdAsync(Guid productId)
    {
        return await _context.ProductVariantAttributes
            .Include(a => a.Values.OrderBy(v => v.DisplayOrder))
            .Where(a => a.ProductId == productId)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductVariantAttribute> AddAttributeAsync(ProductVariantAttribute attribute)
    {
        _context.ProductVariantAttributes.Add(attribute);
        await _context.SaveChangesAsync();
        return attribute;
    }

    /// <inheritdoc />
    public async Task DeleteAttributesByProductIdAsync(Guid productId)
    {
        var attributes = await _context.ProductVariantAttributes
            .Where(a => a.ProductId == productId)
            .ToListAsync();

        if (attributes.Count > 0)
        {
            _context.ProductVariantAttributes.RemoveRange(attributes);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task DeleteVariantsByProductIdAsync(Guid productId)
    {
        var variants = await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .ToListAsync();

        if (variants.Count > 0)
        {
            _context.ProductVariants.RemoveRange(variants);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task AddManyAsync(IEnumerable<ProductVariant> variants)
    {
        _context.ProductVariants.AddRange(variants);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateManyAsync(IEnumerable<ProductVariant> variants)
    {
        _context.ProductVariants.UpdateRange(variants);
        await _context.SaveChangesAsync();
    }
}
