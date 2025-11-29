using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Domain.Interfaces;

/// <summary>
/// Repository interface for review report data access operations.
/// </summary>
public interface IReviewReportRepository
{
    /// <summary>
    /// Gets a review report by its unique identifier.
    /// </summary>
    /// <param name="id">The review report ID.</param>
    /// <returns>The review report if found; otherwise, null.</returns>
    Task<ReviewReport?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all review reports for a specific review.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>A list of review reports for the review.</returns>
    Task<IReadOnlyList<ReviewReport>> GetByReviewIdAsync(Guid reviewId);

    /// <summary>
    /// Checks whether a report exists for a specific review by a reporter.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <param name="reporterId">The reporter ID.</param>
    /// <returns>True if a report exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(Guid reviewId, string reporterId);

    /// <summary>
    /// Adds a new review report to the repository.
    /// </summary>
    /// <param name="report">The review report to add.</param>
    /// <returns>The added review report.</returns>
    Task<ReviewReport> AddAsync(ReviewReport report);
}
