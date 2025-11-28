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
    /// Adds an item to a guest cart.
    /// </summary>
    /// <param name="command">The add to cart command with GuestCartId.</param>
    /// <returns>The result of the add operation.</returns>
    Task<AddToCartResult> AddToGuestCartAsync(AddToCartCommand command);

    /// <summary>
    /// Gets the cart for a buyer.
    /// </summary>
    /// <param name="query">The get cart query.</param>
    /// <returns>The result containing the cart.</returns>
    Task<GetCartResult> GetCartAsync(GetCartQuery query);

    /// <summary>
    /// Gets the cart for a guest.
    /// </summary>
    /// <param name="guestCartId">The guest cart ID.</param>
    /// <returns>The result containing the cart.</returns>
    Task<GetCartResult> GetGuestCartAsync(string guestCartId);

    /// <summary>
    /// Updates the quantity of a cart item.
    /// </summary>
    /// <param name="command">The update quantity command.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateCartItemQuantityResult> UpdateQuantityAsync(UpdateCartItemQuantityCommand command);

    /// <summary>
    /// Updates the quantity of a cart item for a guest.
    /// </summary>
    /// <param name="command">The update quantity command.</param>
    /// <param name="guestCartId">The guest cart ID.</param>
    /// <returns>The result of the update operation.</returns>
    Task<UpdateCartItemQuantityResult> UpdateGuestQuantityAsync(UpdateCartItemQuantityCommand command, string guestCartId);

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    /// <param name="command">The remove item command.</param>
    /// <returns>The result of the remove operation.</returns>
    Task<RemoveCartItemResult> RemoveItemAsync(RemoveCartItemCommand command);

    /// <summary>
    /// Removes an item from a guest cart.
    /// </summary>
    /// <param name="command">The remove item command.</param>
    /// <param name="guestCartId">The guest cart ID.</param>
    /// <returns>The result of the remove operation.</returns>
    Task<RemoveCartItemResult> RemoveGuestItemAsync(RemoveCartItemCommand command, string guestCartId);

    /// <summary>
    /// Gets the total count of items in a buyer's cart.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The total count of items in the cart.</returns>
    Task<int> GetCartItemCountAsync(string buyerId);

    /// <summary>
    /// Gets the total count of items in a guest cart.
    /// </summary>
    /// <param name="guestCartId">The guest cart ID.</param>
    /// <returns>The total count of items in the cart.</returns>
    Task<int> GetGuestCartItemCountAsync(string guestCartId);

    /// <summary>
    /// Merges a guest cart into a user's cart after login.
    /// </summary>
    /// <param name="command">The merge guest cart command.</param>
    /// <returns>The result of the merge operation.</returns>
    Task<MergeGuestCartResult> MergeGuestCartAsync(MergeGuestCartCommand command);
}
