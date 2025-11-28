using Mercato.Cart.Application.Queries;
using Mercato.Cart.Domain.Entities;

namespace Mercato.Cart.Application.Services;

/// <summary>
/// Service interface for calculating shipping costs based on cart contents.
/// </summary>
public interface IShippingCalculator
{
    /// <summary>
    /// Calculates shipping costs for cart items grouped by store.
    /// </summary>
    /// <param name="itemsByStore">The cart items grouped by store.</param>
    /// <returns>A dictionary of store IDs to their shipping costs.</returns>
    Task<IReadOnlyDictionary<Guid, StoreShippingCost>> CalculateShippingAsync(
        IReadOnlyList<CartItemsByStore> itemsByStore);
}
