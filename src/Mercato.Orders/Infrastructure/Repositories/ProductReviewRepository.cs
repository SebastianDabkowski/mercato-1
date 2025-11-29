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
    public async Task<(IReadOnlyList<ProductReview> reviews, int totalCount, double? averageRating)> GetPagedByProductIdAsync(
        Guid productId,
        int page,
        int pageSize,
        ReviewSortOption sortBy)
    {
        var baseQuery = _context.ProductReviews
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Published);

        // Get total count and average rating in a single query using GroupBy
        var aggregateData = await baseQuery
            .GroupBy(r => 1)
            .Select(g => new
            {
                TotalCount = g.Count(),
                AverageRating = g.Average(r => (double?)r.Rating)
            })
            .FirstOrDefaultAsync();

        var totalCount = aggregateData?.TotalCount ?? 0;
        var averageRating = aggregateData?.AverageRating;

        // Apply sorting
        IQueryable<ProductReview> sortedQuery = sortBy switch
        {
            ReviewSortOption.HighestRating => baseQuery
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.CreatedAt),
            ReviewSortOption.LowestRating => baseQuery
                .OrderBy(r => r.Rating)
                .ThenByDescending(r => r.CreatedAt),
            _ => baseQuery.OrderByDescending(r => r.CreatedAt) // Newest is default
        };

        // Apply pagination
        var reviews = await sortedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount, averageRating);
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

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ProductReview> reviews, int totalCount)> GetAllFilteredAsync(
        string? searchText,
        IReadOnlyList<ReviewStatus>? statuses,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize)
    {
        var query = _context.ProductReviews
            .Include(r => r.SellerSubOrderItem)
            .Include(r => r.SellerSubOrder)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(r => r.ReviewText.Contains(searchText) ||
                                     r.BuyerId.Contains(searchText));
        }

        // Apply status filter
        if (statuses != null && statuses.Count > 0)
        {
            query = query.Where(r => statuses.Contains(r.Status));
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply ordering and pagination
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductReview>> GetByStatusAsync(ReviewStatus status)
    {
        return await _context.ProductReviews
            .Include(r => r.SellerSubOrderItem)
            .Include(r => r.SellerSubOrder)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}
