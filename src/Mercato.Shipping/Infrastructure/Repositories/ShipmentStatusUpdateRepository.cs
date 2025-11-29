using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Shipping.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shipment status update data access operations.
/// </summary>
public class ShipmentStatusUpdateRepository : IShipmentStatusUpdateRepository
{
    private readonly ShippingDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShipmentStatusUpdateRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The shipping database context.</param>
    public ShipmentStatusUpdateRepository(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<ShipmentStatusUpdate?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ShipmentStatusUpdates.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShipmentStatusUpdate>> GetByShipmentIdAsync(Guid shipmentId)
    {
        return await _dbContext.ShipmentStatusUpdates
            .Where(u => u.ShipmentId == shipmentId)
            .OrderByDescending(u => u.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ShipmentStatusUpdate?> GetLatestByShipmentIdAsync(Guid shipmentId)
    {
        return await _dbContext.ShipmentStatusUpdates
            .Where(u => u.ShipmentId == shipmentId)
            .OrderByDescending(u => u.Timestamp)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<ShipmentStatusUpdate> AddAsync(ShipmentStatusUpdate statusUpdate)
    {
        await _dbContext.ShipmentStatusUpdates.AddAsync(statusUpdate);
        await _dbContext.SaveChangesAsync();
        return statusUpdate;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<ShipmentStatusUpdate> statusUpdates)
    {
        await _dbContext.ShipmentStatusUpdates.AddRangeAsync(statusUpdates);
        await _dbContext.SaveChangesAsync();
    }
}
