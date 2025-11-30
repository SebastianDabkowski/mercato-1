using Mercato.Identity.Application.Queries;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Interface for providing additional user data from various modules for GDPR data export.
/// </summary>
public interface IUserDataProvider
{
    /// <summary>
    /// Gets delivery addresses for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of delivery address data.</returns>
    Task<IReadOnlyList<DeliveryAddressData>> GetDeliveryAddressesAsync(string userId);

    /// <summary>
    /// Gets order history for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of order data.</returns>
    Task<IReadOnlyList<OrderData>> GetOrdersAsync(string userId);

    /// <summary>
    /// Gets store information for the specified user (if they are a seller).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The store data if the user has a store; otherwise, null.</returns>
    Task<StoreData?> GetStoreAsync(string userId);

    /// <summary>
    /// Gets consent records for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of consent data.</returns>
    Task<IReadOnlyList<ConsentData>> GetConsentsAsync(string userId);
}
