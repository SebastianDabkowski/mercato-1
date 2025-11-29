using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Mercato.Seller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Seller.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shipping method operations.
/// </summary>
public class ShippingMethodRepository : IShippingMethodRepository
{
    private readonly SellerDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingMethodRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ShippingMethodRepository(SellerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ShippingMethod?> GetByIdAsync(Guid id)
    {
        return await _context.ShippingMethods
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingMethod>> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.ShippingMethods
            .Where(m => m.StoreId == storeId)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingMethod>> GetActiveByStoreIdAsync(Guid storeId)
    {
        return await _context.ShippingMethods
            .Where(m => m.StoreId == storeId && m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task AddAsync(ShippingMethod shippingMethod)
    {
        _context.ShippingMethods.Add(shippingMethod);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ShippingMethod shippingMethod)
    {
        _context.ShippingMethods.Update(shippingMethod);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var shippingMethod = await GetByIdAsync(id);
        if (shippingMethod != null)
        {
            _context.ShippingMethods.Remove(shippingMethod);
            await _context.SaveChangesAsync();
        }
    }
}
