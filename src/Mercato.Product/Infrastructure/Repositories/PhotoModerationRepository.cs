using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Product.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for photo moderation data access operations.
/// </summary>
public class PhotoModerationRepository : IPhotoModerationRepository
{
    private readonly ProductDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotoModerationRepository"/> class.
    /// </summary>
    /// <param name="context">The product database context.</param>
    public PhotoModerationRepository(ProductDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<ProductImage> Photos, int TotalCount)> GetPendingPhotosAsync(
        Guid? storeId = null,
        bool flaggedOnly = false,
        int skip = 0,
        int take = 20)
    {
        var query = _context.ProductImages
            .AsNoTracking()
            .Include(pi => pi.Product)
            .Where(pi => pi.ModerationStatus == PhotoModerationStatus.PendingReview);

        if (storeId.HasValue)
        {
            query = query.Where(pi => pi.Product != null && pi.Product.StoreId == storeId.Value);
        }

        if (flaggedOnly)
        {
            query = query.Where(pi => pi.IsFlagged);
        }

        var totalCount = await query.CountAsync();

        var photos = await query
            .OrderByDescending(pi => pi.IsFlagged)
            .ThenBy(pi => pi.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (photos, totalCount);
    }

    /// <inheritdoc/>
    public async Task<ProductImage?> GetPhotoByIdAsync(Guid imageId)
    {
        return await _context.ProductImages
            .Include(pi => pi.Product)
            .FirstOrDefaultAsync(pi => pi.Id == imageId);
    }

    /// <inheritdoc/>
    public async Task UpdatePhotoModerationStatusAsync(ProductImage image)
    {
        _context.ProductImages.Update(image);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task AddModerationDecisionAsync(PhotoModerationDecision decision)
    {
        await _context.PhotoModerationDecisions.AddAsync(decision);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PhotoModerationDecision>> GetModerationHistoryAsync(Guid imageId)
    {
        return await _context.PhotoModerationDecisions
            .Where(d => d.ProductImageId == imageId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> FlagPhotoAsync(Guid imageId, string reason)
    {
        var image = await _context.ProductImages.FindAsync(imageId);
        if (image == null)
        {
            return false;
        }

        image.IsFlagged = true;
        image.FlagReason = reason;
        image.FlaggedAt = DateTimeOffset.UtcNow;
        image.ModerationStatus = PhotoModerationStatus.PendingReview;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingPhotoCountAsync()
    {
        return await _context.ProductImages
            .CountAsync(pi => pi.ModerationStatus == PhotoModerationStatus.PendingReview);
    }
}
