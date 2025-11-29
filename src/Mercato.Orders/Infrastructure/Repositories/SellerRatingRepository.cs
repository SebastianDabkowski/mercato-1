using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for seller rating data access operations.
/// </summary>
public class SellerRatingRepository : ISellerRatingRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SellerRatingRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SellerRatingRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SellerRating?> GetByIdAsync(Guid id)
    {
        return await _context.SellerRatings
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Order)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerRating>> GetByBuyerIdAsync(string buyerId)
    {
        return await _context.SellerRatings
            .Include(r => r.SellerSubOrder)
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerRating>> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.SellerRatings
            .Include(r => r.SellerSubOrder)
            .Where(r => r.StoreId == storeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SellerRating> AddAsync(SellerRating rating)
    {
        await _context.SellerRatings.AddAsync(rating);
        await _context.SaveChangesAsync();
        return rating;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SellerRating rating)
    {
        _context.SellerRatings.Update(rating);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForSubOrderAsync(Guid sellerSubOrderId, string buyerId)
    {
        return await _context.SellerRatings
            .AnyAsync(r => r.SellerSubOrderId == sellerSubOrderId && r.BuyerId == buyerId);
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetBuyerLastRatingTimeAsync(string buyerId)
    {
        return await _context.SellerRatings
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (DateTimeOffset?)r.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<double?> GetAverageRatingForStoreAsync(Guid storeId)
    {
        return await _context.SellerRatings
            .Where(r => r.StoreId == storeId)
            .Select(r => (double?)r.Rating)
            .AverageAsync();
    }
}
