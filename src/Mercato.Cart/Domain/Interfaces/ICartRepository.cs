using Mercato.Cart.Domain.Entities;

namespace Mercato.Cart.Domain.Interfaces;

/// <summary>
/// Repository interface for cart data access operations.
/// </summary>
public interface ICartRepository
{
    /// <summary>
    /// Gets a cart by the buyer ID.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>The cart if found; otherwise, null.</returns>
    Task<Entities.Cart?> GetByBuyerIdAsync(string buyerId);

    /// <summary>
    /// Gets a cart by its unique identifier.
    /// </summary>
    /// <param name="id">The cart ID.</param>
    /// <returns>The cart if found; otherwise, null.</returns>
    Task<Entities.Cart?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds a new cart to the repository.
    /// </summary>
    /// <param name="cart">The cart to add.</param>
    /// <returns>The added cart.</returns>
    Task<Entities.Cart> AddAsync(Entities.Cart cart);

    /// <summary>
    /// Updates an existing cart.
    /// </summary>
    /// <param name="cart">The cart to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Entities.Cart cart);

    /// <summary>
    /// Adds an item to a cart.
    /// </summary>
    /// <param name="item">The cart item to add.</param>
    /// <returns>The added cart item.</returns>
    Task<CartItem> AddItemAsync(CartItem item);

    /// <summary>
    /// Updates a cart item.
    /// </summary>
    /// <param name="item">The cart item to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateItemAsync(CartItem item);

    /// <summary>
    /// Removes a cart item.
    /// </summary>
    /// <param name="item">The cart item to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveItemAsync(CartItem item);

    /// <summary>
    /// Gets a cart item by product ID for a specific cart.
    /// </summary>
    /// <param name="cartId">The cart ID.</param>
    /// <param name="productId">The product ID.</param>
    /// <returns>The cart item if found; otherwise, null.</returns>
    Task<CartItem?> GetItemByProductIdAsync(Guid cartId, Guid productId);

    /// <summary>
    /// Gets a cart item by its unique identifier.
    /// </summary>
    /// <param name="itemId">The cart item ID.</param>
    /// <returns>The cart item if found; otherwise, null.</returns>
    Task<CartItem?> GetItemByIdAsync(Guid itemId);
}
