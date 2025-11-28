using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for product image data access operations.
/// </summary>
public class ProductImageRepository : IProductImageRepository
{
    private readonly ProductDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductImageRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ProductImageRepository(ProductDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ProductImage> AddAsync(ProductImage image)
    {
        _context.ProductImages.Add(image);
        await _context.SaveChangesAsync();
        return image;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(Guid productId)
    {
        return await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductImage?> GetByIdAsync(Guid id)
    {
        return await _context.ProductImages.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ProductImage image)
    {
        _context.ProductImages.Update(image);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var image = await _context.ProductImages.FindAsync(id);
        if (image != null)
        {
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task SetMainImageAsync(Guid productId, Guid imageId)
    {
        // Clear main flag from all images of this product
        var images = await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .ToListAsync();

        foreach (var image in images)
        {
            image.IsMain = image.Id == imageId;
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetImageCountByProductIdAsync(Guid productId)
    {
        return await _context.ProductImages
            .CountAsync(i => i.ProductId == productId);
    }
}
