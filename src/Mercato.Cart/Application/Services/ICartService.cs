using Mercato.Cart.Application.Commands;
using Mercato.Cart.Application.Queries;

namespace Mercato.Cart.Application.Services;

/// <summary>
/// Service interface for shopping cart operations.
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Adds an item to the cart.
    /// </summary>
    /// <param name="command">The add to cart command.</param>
    /// <returns>The result of the add operation.</returns>
    Task<AddToCartResult> AddToCartAsync(AddToCartCommand command);

    /// <summary>
    /// Gets the cart for a buyer.
    /// </summary>
    /// <param name="query">The get cart query.</param>
    /// <returns>The result containing the cart.</returns>
    Task<GetCartResult> GetCartAsync(GetCartQuery query);

    /// <summary>
    /// Updates the quantity of a cart item.
    /// </summary>
    /// <param name="command">The update quantity command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateCartItemQuantityResult> UpdateQuantityAsync(UpdateCartItemQuantityCommand command);

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    /// <param name="command">The remove item command.</param>
    /// <returns>The result of the remove operation.</returns>
    Task<RemoveCartItemResult> RemoveItemAsync(RemoveCartItemCommand command);

    /// <summary>
    /// Gets the total count of items in a buyer's cart.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The total count of items in the cart.</returns>
    Task<int> GetCartItemCountAsync(string buyerId);
}
