using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Shipping.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for store shipping provider data access operations.
/// </summary>
public class StoreShippingProviderRepository : IStoreShippingProviderRepository
{
    private readonly ShippingDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreShippingProviderRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The shipping database context.</param>
    public StoreShippingProviderRepository(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<StoreShippingProvider?> GetByIdAsync(Guid id)
    {
        return await _dbContext.StoreShippingProviders
            .Include(s => s.ShippingProvider)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreShippingProvider>> GetByStoreIdAsync(Guid storeId)
    {
        return await _dbContext.StoreShippingProviders
            .Include(s => s.ShippingProvider)
            .Where(s => s.StoreId == storeId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreShippingProvider>> GetEnabledByStoreIdAsync(Guid storeId)
    {
        return await _dbContext.StoreShippingProviders
            .Include(s => s.ShippingProvider)
            .Where(s => s.StoreId == storeId && s.IsEnabled)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<StoreShippingProvider?> GetByStoreAndProviderAsync(Guid storeId, Guid shippingProviderId)
    {
        return await _dbContext.StoreShippingProviders
            .Include(s => s.ShippingProvider)
            .FirstOrDefaultAsync(s => s.StoreId == storeId && s.ShippingProviderId == shippingProviderId);
    }

    /// <inheritdoc />
    public async Task<StoreShippingProvider> AddAsync(StoreShippingProvider storeShippingProvider)
    {
        await _dbContext.StoreShippingProviders.AddAsync(storeShippingProvider);
        await _dbContext.SaveChangesAsync();
        return storeShippingProvider;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(StoreShippingProvider storeShippingProvider)
    {
        _dbContext.StoreShippingProviders.Update(storeShippingProvider);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var storeProvider = await _dbContext.StoreShippingProviders.FindAsync(id);
        if (storeProvider != null)
        {
            _dbContext.StoreShippingProviders.Remove(storeProvider);
            await _dbContext.SaveChangesAsync();
        }
    }
}
