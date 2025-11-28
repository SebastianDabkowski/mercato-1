using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;

namespace Mercato.Buyer.Application.Services;

/// <summary>
/// Service interface for delivery address operations.
/// </summary>
public interface IDeliveryAddressService
{
    /// <summary>
    /// Gets the list of allowed shipping countries as ISO codes.
    /// </summary>
    IReadOnlyList<string> AllowedShippingCountries { get; }

    /// <summary>
    /// Gets all delivery addresses for a buyer.
    /// </summary>
    /// <param name="query">The query containing the buyer ID.</param>
    /// <returns>The result containing the delivery addresses.</returns>
    Task<GetDeliveryAddressesResult> GetAddressesAsync(GetDeliveryAddressesQuery query);

    /// <summary>
    /// Saves (adds or updates) a delivery address.
    /// </summary>
    /// <param name="command">The command containing address details.</param>
    /// <returns>The result of the save operation.</returns>
    Task<SaveDeliveryAddressResult> SaveAddressAsync(SaveDeliveryAddressCommand command);

    /// <summary>
    /// Deletes a delivery address.
    /// </summary>
    /// <param name="command">The command containing the address ID to delete.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<DeleteDeliveryAddressResult> DeleteAddressAsync(DeleteDeliveryAddressCommand command);

    /// <summary>
    /// Sets a delivery address as the default.
    /// </summary>
    /// <param name="command">The command containing the address ID to set as default.</param>
    /// <returns>The result of the set default operation.</returns>
    Task<SetDefaultDeliveryAddressResult> SetDefaultAddressAsync(SetDefaultDeliveryAddressCommand command);

    /// <summary>
    /// Validates if shipping is allowed to the specified country.
    /// </summary>
    /// <param name="countryCode">The ISO country code to validate.</param>
    /// <returns>True if shipping is allowed; otherwise, false.</returns>
    bool IsShippingAllowedToRegion(string countryCode);
}
