using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for store data access.
/// </summary>
public class StoreRepository : IStoreRepository
{
    private readonly SellerDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreRepository"/> class.
    /// </summary>
    /// <param name="context">The seller database context.</param>
    public StoreRepository(SellerDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Store?> GetBySellerIdAsync(string sellerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);

        return await _context.Stores
            .FirstOrDefaultAsync(s => s.SellerId == sellerId);
    }

    /// <inheritdoc />
    public async Task<Store?> GetByIdAsync(Guid id)
    {
        return await _context.Stores.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<Store?> GetBySlugAsync(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return await _context.Stores
            .FirstOrDefaultAsync(s => s.Slug == slug);
    }

    /// <inheritdoc />
    public async Task<bool> IsStoreNameUniqueAsync(string name, string? excludeSellerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var query = _context.Stores.Where(s => s.Name == name);

        if (!string.IsNullOrWhiteSpace(excludeSellerId))
        {
            query = query.Where(s => s.SellerId != excludeSellerId);
        }

        return !await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsSlugUniqueAsync(string slug, string? excludeSellerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var query = _context.Stores.Where(s => s.Slug == slug);

        if (!string.IsNullOrWhiteSpace(excludeSellerId))
        {
            query = query.Where(s => s.SellerId != excludeSellerId);
        }

        return !await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task CreateAsync(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _context.Stores.Update(store);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Store>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await _context.Stores
            .Where(s => idList.Contains(s.Id))
            .ToListAsync();
    }
}
