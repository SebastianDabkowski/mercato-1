using Mercato.Cart.Domain.Entities;
using Mercato.Cart.Domain.Interfaces;
using Mercato.Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Cart.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for cart data access operations.
/// </summary>
public class CartRepository : ICartRepository
{
    private readonly CartDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartRepository"/> class.
    /// </summary>
    /// <param name="context">The cart database context.</param>
    public CartRepository(CartDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Cart?> GetByBuyerIdAsync(string buyerId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId);
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Cart?> GetByGuestCartIdAsync(string guestCartId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.GuestCartId == guestCartId);
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Cart?> GetByIdAsync(Guid id)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Cart> AddAsync(Domain.Entities.Cart cart)
    {
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Domain.Entities.Cart cart)
    {
        _context.Carts.Update(cart);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Domain.Entities.Cart cart)
    {
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<CartItem> AddItemAsync(CartItem item)
    {
        _context.CartItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    /// <inheritdoc />
    public async Task UpdateItemAsync(CartItem item)
    {
        _context.CartItems.Update(item);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveItemAsync(CartItem item)
    {
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<CartItem?> GetItemByProductIdAsync(Guid cartId, Guid productId)
    {
        return await _context.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductId == productId);
    }

    /// <inheritdoc />
    public async Task<CartItem?> GetItemByIdAsync(Guid itemId)
    {
        return await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == itemId);
    }
}
