using Mercato.Product.Application.Queries;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for product data access operations.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Entities.Product>> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.Products
            .Where(p => p.StoreId == storeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Product> AddAsync(Domain.Entities.Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Domain.Entities.Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Entities.Product>> GetActiveByStoreIdAsync(Guid storeId)
    {
        return await _context.Products
            .Where(p => p.StoreId == storeId && p.Status != ProductStatus.Archived)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain.Entities.Product>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        return await _context.Products
            .Where(p => idList.Contains(p.Id))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdateManyAsync(IEnumerable<Domain.Entities.Product> products)
    {
        _context.Products.UpdateRange(products);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Domain.Entities.Product> Products, int TotalCount)> GetActiveByCategoryAsync(string categoryName, int page, int pageSize)
    {
        var query = _context.Products
            .Where(p => p.Category == categoryName && p.Status == ProductStatus.Active);

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Domain.Entities.Product> Products, int TotalCount)> SearchActiveProductsAsync(string searchQuery, int page, int pageSize)
    {
        // Use LIKE pattern for SQL Server compatibility
        var likePattern = $"%{searchQuery}%";

        var query = _context.Products
            .Where(p => p.Status == ProductStatus.Active &&
                       (EF.Functions.Like(p.Title, likePattern) ||
                        (p.Description != null && EF.Functions.Like(p.Description, likePattern))));

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Domain.Entities.Product> Products, int TotalCount)> SearchActiveProductsWithFiltersAsync(
        string? searchQuery,
        string? categoryName,
        decimal? minPrice,
        decimal? maxPrice,
        string? condition,
        Guid? storeId,
        int page,
        int pageSize,
        ProductSortOption sortBy = ProductSortOption.Relevance)
    {
        var query = _context.Products
            .Where(p => p.Status == ProductStatus.Active);

        // Apply search query filter
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var likePattern = $"%{searchQuery}%";
            query = query.Where(p => 
                EF.Functions.Like(p.Title, likePattern) ||
                (p.Description != null && EF.Functions.Like(p.Description, likePattern)));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            query = query.Where(p => p.Category == categoryName);
        }

        // Apply price range filters
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // Apply condition filter (InStock or OutOfStock)
        if (!string.IsNullOrWhiteSpace(condition))
        {
            if (condition.Equals("InStock", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Stock > 0);
            }
            else if (condition.Equals("OutOfStock", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Stock <= 0);
            }
        }

        // Apply store/seller filter
        if (storeId.HasValue)
        {
            query = query.Where(p => p.StoreId == storeId.Value);
        }

        var totalCount = await query.CountAsync();

        // Apply sorting
        IOrderedQueryable<Domain.Entities.Product> orderedQuery = sortBy switch
        {
            ProductSortOption.PriceAsc => query.OrderBy(p => p.Price).ThenByDescending(p => p.CreatedAt),
            ProductSortOption.PriceDesc => query.OrderByDescending(p => p.Price).ThenByDescending(p => p.CreatedAt),
            ProductSortOption.Newest => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            // Relevance: For search queries, we could potentially implement relevance scoring,
            // but for MVP we default to newest first. This provides a stable, consistent sort.
            _ => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
        };

        var products = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    /// <inheritdoc />
    public async Task<(decimal? MinPrice, decimal? MaxPrice)> GetActivePriceRangeAsync()
    {
        var query = _context.Products
            .Where(p => p.Status == ProductStatus.Active);

        if (!await query.AnyAsync())
        {
            return (null, null);
        }

        var minPrice = await query.MinAsync(p => p.Price);
        var maxPrice = await query.MaxAsync(p => p.Price);

        return (minPrice, maxPrice);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetActiveProductStoreIdsAsync()
    {
        return await _context.Products
            .Where(p => p.Status == ProductStatus.Active)
            .Select(p => p.StoreId)
            .Distinct()
            .ToListAsync();
    }
}
