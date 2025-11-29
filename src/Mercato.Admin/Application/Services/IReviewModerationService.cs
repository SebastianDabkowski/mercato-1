using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for admin review moderation operations.
/// </summary>
public interface IReviewModerationService
{
    /// <summary>
    /// Gets filtered and paginated reviews for admin moderation view.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the filtered reviews.</returns>
    Task<GetAdminReviewsResult> GetReviewsAsync(AdminReviewFilterQuery query);

    /// <summary>
    /// Gets full details of a specific review.
    /// </summary>
    /// <param name="reviewId">The review ID.</param>
    /// <returns>The result containing the review details.</returns>
    Task<GetAdminReviewDetailsResult> GetReviewDetailsAsync(Guid reviewId);

    /// <summary>
    /// Moderates a review by changing its status.
    /// </summary>
    /// <param name="command">The moderation command.</param>
    /// <returns>The result of the moderation operation.</returns>
    Task<ModerateReviewResult> ModerateReviewAsync(ModerateReviewCommand command);

    /// <summary>
    /// Flags a review for moderation (sets status to Pending).
    /// </summary>
    /// <param name="command">The flag command.</param>
    /// <returns>The result of the flagging operation.</returns>
    Task<FlagReviewResult> FlagReviewAsync(FlagReviewCommand command);
}
