using Mercato.Shipping.Domain.Entities;

namespace Mercato.Shipping.Domain.Interfaces;

/// <summary>
/// Repository interface for shipping provider data access operations.
/// </summary>
public interface IShippingProviderRepository
{
    /// <summary>
    /// Gets a shipping provider by its unique identifier.
    /// </summary>
    /// <param name="id">The shipping provider identifier.</param>
    /// <returns>The shipping provider if found; otherwise, null.</returns>
    Task<ShippingProvider?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a shipping provider by its unique code.
    /// </summary>
    /// <param name="code">The shipping provider code (e.g., "DHL", "FEDEX").</param>
    /// <returns>The shipping provider if found; otherwise, null.</returns>
    Task<ShippingProvider?> GetByCodeAsync(string code);

    /// <summary>
    /// Gets all shipping providers on the platform.
    /// </summary>
    /// <returns>A read-only list of all shipping providers.</returns>
    Task<IReadOnlyList<ShippingProvider>> GetAllAsync();

    /// <summary>
    /// Gets all active shipping providers.
    /// </summary>
    /// <returns>A read-only list of active shipping providers.</returns>
    Task<IReadOnlyList<ShippingProvider>> GetActiveProvidersAsync();

    /// <summary>
    /// Adds a new shipping provider.
    /// </summary>
    /// <param name="provider">The shipping provider to add.</param>
    /// <returns>The added shipping provider.</returns>
    Task<ShippingProvider> AddAsync(ShippingProvider provider);

    /// <summary>
    /// Updates an existing shipping provider.
    /// </summary>
    /// <param name="provider">The shipping provider to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(ShippingProvider provider);
}
