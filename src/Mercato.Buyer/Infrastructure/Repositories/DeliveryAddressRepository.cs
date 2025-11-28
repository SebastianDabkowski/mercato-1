using Mercato.Buyer.Domain.Entities;
using Mercato.Buyer.Domain.Interfaces;
using Mercato.Buyer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Buyer.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for delivery address data access operations.
/// </summary>
public class DeliveryAddressRepository : IDeliveryAddressRepository
{
    private readonly BuyerDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryAddressRepository"/> class.
    /// </summary>
    /// <param name="context">The buyer database context.</param>
    public DeliveryAddressRepository(BuyerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<DeliveryAddress?> GetByIdAsync(Guid id)
    {
        return await _context.DeliveryAddresses
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeliveryAddress>> GetByBuyerIdAsync(string buyerId)
    {
        return await _context.DeliveryAddresses
            .Where(a => a.BuyerId == buyerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.LastUpdatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<DeliveryAddress?> GetDefaultByBuyerIdAsync(string buyerId)
    {
        return await _context.DeliveryAddresses
            .FirstOrDefaultAsync(a => a.BuyerId == buyerId && a.IsDefault);
    }

    /// <inheritdoc />
    public async Task<DeliveryAddress> AddAsync(DeliveryAddress address)
    {
        _context.DeliveryAddresses.Add(address);
        await _context.SaveChangesAsync();
        return address;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(DeliveryAddress address)
    {
        _context.DeliveryAddresses.Update(address);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(DeliveryAddress address)
    {
        _context.DeliveryAddresses.Remove(address);
        await _context.SaveChangesAsync();
    }
}
