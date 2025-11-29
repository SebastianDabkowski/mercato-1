using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Shipping.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shipping label data access operations.
/// </summary>
public class ShippingLabelRepository : IShippingLabelRepository
{
    private readonly ShippingDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingLabelRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The shipping database context.</param>
    public ShippingLabelRepository(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<ShippingLabel?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ShippingLabels
            .Include(l => l.Shipment)
                .ThenInclude(s => s.StoreShippingProvider)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    /// <inheritdoc />
    public async Task<ShippingLabel?> GetByShipmentIdAsync(Guid shipmentId)
    {
        return await _dbContext.ShippingLabels
            .Include(l => l.Shipment)
                .ThenInclude(s => s.StoreShippingProvider)
            .FirstOrDefaultAsync(l => l.ShipmentId == shipmentId);
    }

    /// <inheritdoc />
    public async Task<ShippingLabel> AddAsync(ShippingLabel label)
    {
        await _dbContext.ShippingLabels.AddAsync(label);
        await _dbContext.SaveChangesAsync();
        return label;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        var label = await _dbContext.ShippingLabels.FindAsync(id);
        if (label != null)
        {
            _dbContext.ShippingLabels.Remove(label);
            await _dbContext.SaveChangesAsync();
        }
    }
}
