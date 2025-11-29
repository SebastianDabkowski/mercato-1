using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for product review data access operations.
/// </summary>
public class ProductReviewRepository : IProductReviewRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductReviewRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ProductReviewRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ProductReview?> GetByIdAsync(Guid id)
    {
        return await _context.ProductReviews
            .Include(r => r.SellerSubOrderItem)
            .Include(r => r.SellerSubOrder)
                .ThenInclude(s => s.Order)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductReview>> GetByBuyerIdAsync(string buyerId)
    {
        return await _context.ProductReviews
            .Include(r => r.SellerSubOrderItem)
            .Include(r => r.SellerSubOrder)
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(Guid productId)
    {
        return await _context.ProductReviews
            .Include(r => r.SellerSubOrderItem)
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Published)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductReview>> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.ProductReviews
            .Include(r => r.SellerSubOrderItem)
            .Include(r => r.SellerSubOrder)
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductReview?> GetBySellerSubOrderItemIdAsync(Guid itemId)
    {
        return await _context.ProductReviews
            .Include(r => r.SellerSubOrderItem)
            .Include(r => r.SellerSubOrder)
            .FirstOrDefaultAsync(r => r.SellerSubOrderItemId == itemId);
    }

    /// <inheritdoc />
    public async Task<ProductReview> AddAsync(ProductReview review)
    {
        await _context.ProductReviews.AddAsync(review);
        await _context.SaveChangesAsync();
        return review;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ProductReview review)
    {
        _context.ProductReviews.Update(review);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForItemAsync(Guid sellerSubOrderItemId, string buyerId)
    {
        return await _context.ProductReviews
            .AnyAsync(r => r.SellerSubOrderItemId == sellerSubOrderItemId && r.BuyerId == buyerId);
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetBuyerLastReviewTimeAsync(string buyerId)
    {
        return await _context.ProductReviews
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (DateTimeOffset?)r.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
