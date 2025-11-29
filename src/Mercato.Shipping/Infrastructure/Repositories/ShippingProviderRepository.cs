using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Shipping.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shipping provider data access operations.
/// </summary>
public class ShippingProviderRepository : IShippingProviderRepository
{
    private readonly ShippingDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingProviderRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The shipping database context.</param>
    public ShippingProviderRepository(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<ShippingProvider?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ShippingProviders.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<ShippingProvider?> GetByCodeAsync(string code)
    {
        return await _dbContext.ShippingProviders
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingProvider>> GetAllAsync()
    {
        return await _dbContext.ShippingProviders.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingProvider>> GetActiveProvidersAsync()
    {
        return await _dbContext.ShippingProviders
            .Where(p => p.IsActive && p.Status == ShippingProviderStatus.Active)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ShippingProvider> AddAsync(ShippingProvider provider)
    {
        await _dbContext.ShippingProviders.AddAsync(provider);
        await _dbContext.SaveChangesAsync();
        return provider;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ShippingProvider provider)
    {
        _dbContext.ShippingProviders.Update(provider);
        await _dbContext.SaveChangesAsync();
    }
}
