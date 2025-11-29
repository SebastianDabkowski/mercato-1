using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Domain.Interfaces;

/// <summary>
/// Repository interface for shipment data access operations.
/// </summary>
public interface IShipmentRepository
{
    /// <summary>
    /// Gets a shipment by its unique identifier.
    /// </summary>
    /// <param name="id">The shipment identifier.</param>
    /// <returns>The shipment if found; otherwise, null.</returns>
    Task<Shipment?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a shipment by its tracking number.
    /// </summary>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <returns>The shipment if found; otherwise, null.</returns>
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber);

    /// <summary>
    /// Gets a shipment by its external shipment ID from the provider.
    /// </summary>
    /// <param name="externalShipmentId">The external shipment identifier.</param>
    /// <returns>The shipment if found; otherwise, null.</returns>
    Task<Shipment?> GetByExternalShipmentIdAsync(string externalShipmentId);

    /// <summary>
    /// Gets all shipments for a seller sub-order.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order identifier.</param>
    /// <returns>A read-only list of shipments for the sub-order.</returns>
    Task<IReadOnlyList<Shipment>> GetBySellerSubOrderIdAsync(Guid sellerSubOrderId);

    /// <summary>
    /// Gets shipments by store shipping provider.
    /// </summary>
    /// <param name="storeShippingProviderId">The store shipping provider identifier.</param>
    /// <returns>A read-only list of shipments created with this provider.</returns>
    Task<IReadOnlyList<Shipment>> GetByStoreShippingProviderIdAsync(Guid storeShippingProviderId);

    /// <summary>
    /// Gets shipments by status.
    /// </summary>
    /// <param name="status">The shipment status.</param>
    /// <returns>A read-only list of shipments with the specified status.</returns>
    Task<IReadOnlyList<Shipment>> GetByStatusAsync(ShipmentStatus status);

    /// <summary>
    /// Gets shipments that are in transit and need status polling.
    /// </summary>
    /// <returns>A read-only list of shipments that are in-transit statuses.</returns>
    Task<IReadOnlyList<Shipment>> GetShipmentsForPollingAsync();

    /// <summary>
    /// Adds a new shipment.
    /// </summary>
    /// <param name="shipment">The shipment to add.</param>
    /// <returns>The added shipment.</returns>
    Task<Shipment> AddAsync(Shipment shipment);

    /// <summary>
    /// Updates an existing shipment.
    /// </summary>
    /// <param name="shipment">The shipment to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Shipment shipment);
}
