using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for product import job data access operations.
/// </summary>
public class ProductImportRepository : IProductImportRepository
{
    private readonly ProductDbContext _context;

    public ProductImportRepository(ProductDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ProductImportJob> AddAsync(ProductImportJob job)
    {
        _context.ProductImportJobs.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    /// <inheritdoc />
    public async Task<ProductImportJob?> GetByIdAsync(Guid id)
    {
        return await _context.ProductImportJobs.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductImportJob>> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.ProductImportJobs
            .Where(j => j.StoreId == storeId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ProductImportJob job)
    {
        _context.ProductImportJobs.Update(job);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task AddRowErrorsAsync(IEnumerable<ProductImportRowError> errors)
    {
        _context.ProductImportRowErrors.AddRange(errors);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductImportRowError>> GetRowErrorsByJobIdAsync(Guid jobId)
    {
        return await _context.ProductImportRowErrors
            .Where(e => e.ImportJobId == jobId)
            .OrderBy(e => e.RowNumber)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Product?> GetProductBySkuAsync(Guid storeId, string sku)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.StoreId == storeId && p.Sku == sku);
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, Domain.Entities.Product>> GetProductsBySkusAsync(Guid storeId, IEnumerable<string> skus)
    {
        var skuList = skus.ToList();
        var products = await _context.Products
            .Where(p => p.StoreId == storeId && p.Sku != null && skuList.Contains(p.Sku))
            .ToListAsync();

        return products.Where(p => p.Sku != null).ToDictionary(p => p.Sku!);
    }
}
