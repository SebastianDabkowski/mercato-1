using Mercato.Orders.Domain.Entities;
using Mercato.Orders.Domain.Interfaces;
using Mercato.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Orders.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for review report data access operations.
/// </summary>
public class ReviewReportRepository : IReviewReportRepository
{
    private readonly OrderDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewReportRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ReviewReportRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ReviewReport?> GetByIdAsync(Guid id)
    {
        return await _context.ReviewReports
            .Include(r => r.Review)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReviewReport>> GetByReviewIdAsync(Guid reviewId)
    {
        return await _context.ReviewReports
            .Include(r => r.Review)
            .Where(r => r.ReviewId == reviewId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid reviewId, string reporterId)
    {
        return await _context.ReviewReports
            .AnyAsync(r => r.ReviewId == reviewId && r.ReporterId == reporterId);
    }

    /// <inheritdoc />
    public async Task<ReviewReport> AddAsync(ReviewReport report)
    {
        await _context.ReviewReports.AddAsync(report);
        await _context.SaveChangesAsync();
        return report;
    }
}
