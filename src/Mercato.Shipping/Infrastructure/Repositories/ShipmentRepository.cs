using Mercato.Shipping.Domain.Entities;
using Mercato.Shipping.Domain.Interfaces;
using Mercato.Shipping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Shipping.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shipment data access operations.
/// </summary>
public class ShipmentRepository : IShipmentRepository
{
    private readonly ShippingDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShipmentRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The shipping database context.</param>
    public ShipmentRepository(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Shipment?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Shipments
            .Include(s => s.StoreShippingProvider)
                .ThenInclude(sp => sp.ShippingProvider)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber)
    {
        return await _dbContext.Shipments
            .Include(s => s.StoreShippingProvider)
                .ThenInclude(sp => sp.ShippingProvider)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);
    }

    /// <inheritdoc />
    public async Task<Shipment?> GetByExternalShipmentIdAsync(string externalShipmentId)
    {
        return await _dbContext.Shipments
            .Include(s => s.StoreShippingProvider)
                .ThenInclude(sp => sp.ShippingProvider)
            .FirstOrDefaultAsync(s => s.ExternalShipmentId == externalShipmentId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Shipment>> GetBySellerSubOrderIdAsync(Guid sellerSubOrderId)
    {
        return await _dbContext.Shipments
            .Include(s => s.StoreShippingProvider)
                .ThenInclude(sp => sp.ShippingProvider)
            .Where(s => s.SellerSubOrderId == sellerSubOrderId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Shipment>> GetByStoreShippingProviderIdAsync(Guid storeShippingProviderId)
    {
        return await _dbContext.Shipments
            .Include(s => s.StoreShippingProvider)
                .ThenInclude(sp => sp.ShippingProvider)
            .Where(s => s.StoreShippingProviderId == storeShippingProviderId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Shipment>> GetByStatusAsync(ShipmentStatus status)
    {
        return await _dbContext.Shipments
            .Include(s => s.StoreShippingProvider)
                .ThenInclude(sp => sp.ShippingProvider)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Shipment>> GetShipmentsForPollingAsync()
    {
        // Get shipments that are in states where we expect updates
        var pollableStatuses = new[]
        {
            ShipmentStatus.Created,
            ShipmentStatus.PickedUp,
            ShipmentStatus.InTransit,
            ShipmentStatus.OutForDelivery
        };

        return await _dbContext.Shipments
            .Include(s => s.StoreShippingProvider)
                .ThenInclude(sp => sp.ShippingProvider)
            .Where(s => pollableStatuses.Contains(s.Status))
            .OrderBy(s => s.LastUpdatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Shipment> AddAsync(Shipment shipment)
    {
        await _dbContext.Shipments.AddAsync(shipment);
        await _dbContext.SaveChangesAsync();
        return shipment;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Shipment shipment)
    {
        _dbContext.Shipments.Update(shipment);
        await _dbContext.SaveChangesAsync();
    }
}
