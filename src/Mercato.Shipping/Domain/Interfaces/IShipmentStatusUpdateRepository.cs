using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Domain.Interfaces;

/// <summary>
/// Repository interface for shipment status update data access operations.
/// </summary>
public interface IShipmentStatusUpdateRepository
{
    /// <summary>
    /// Gets a shipment status update by its unique identifier.
    /// </summary>
    /// <param name="id">The status update identifier.</param>
    /// <returns>The status update if found; otherwise, null.</returns>
    Task<ShipmentStatusUpdate?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all status updates for a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <returns>A read-only list of status updates ordered by timestamp.</returns>
    Task<IReadOnlyList<ShipmentStatusUpdate>> GetByShipmentIdAsync(Guid shipmentId);

    /// <summary>
    /// Gets the most recent status update for a shipment.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <returns>The most recent status update if any; otherwise, null.</returns>
    Task<ShipmentStatusUpdate?> GetLatestByShipmentIdAsync(Guid shipmentId);

    /// <summary>
    /// Adds a new shipment status update.
    /// </summary>
    /// <param name="statusUpdate">The status update to add.</param>
    /// <returns>The added status update.</returns>
    Task<ShipmentStatusUpdate> AddAsync(ShipmentStatusUpdate statusUpdate);

    /// <summary>
    /// Adds multiple shipment status updates.
    /// </summary>
    /// <param name="statusUpdates">The status updates to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<ShipmentStatusUpdate> statusUpdates);
}
