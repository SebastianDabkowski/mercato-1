using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Domain.Interfaces;

/// <summary>
/// Repository interface for shipping label data access operations.
/// </summary>
public interface IShippingLabelRepository
{
    /// <summary>
    /// Gets a shipping label by its unique identifier.
    /// </summary>
    /// <param name="id">The label identifier.</param>
    /// <returns>The shipping label if found; otherwise, null.</returns>
    Task<ShippingLabel?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a shipping label by shipment identifier.
    /// </summary>
    /// <param name="shipmentId">The shipment identifier.</param>
    /// <returns>The shipping label if found; otherwise, null.</returns>
    Task<ShippingLabel?> GetByShipmentIdAsync(Guid shipmentId);

    /// <summary>
    /// Adds a new shipping label.
    /// </summary>
    /// <param name="label">The shipping label to add.</param>
    /// <returns>The added shipping label.</returns>
    Task<ShippingLabel> AddAsync(ShippingLabel label);

    /// <summary>
    /// Deletes a shipping label by its identifier.
    /// </summary>
    /// <param name="id">The label identifier to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id);
}
