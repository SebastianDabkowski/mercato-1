using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;

namespace Mercato.Admin.Application.Services;

/// <summary>
/// Service interface for admin photo moderation operations.
/// </summary>
public interface IPhotoModerationService
{
    /// <summary>
    /// Gets filtered and paginated photos for admin moderation view.
    /// </summary>
    /// <param name="query">The filter query parameters.</param>
    /// <returns>The result containing the filtered photos.</returns>
    Task<GetPhotosForModerationResult> GetPhotosForModerationAsync(PhotoModerationFilterQuery query);

    /// <summary>
    /// Gets full details of a specific photo for moderation.
    /// </summary>
    /// <param name="imageId">The image ID.</param>
    /// <returns>The result containing the photo details.</returns>
    Task<GetPhotoModerationDetailsResult> GetPhotoDetailsAsync(Guid imageId);

    /// <summary>
    /// Approves a photo, making it visible on the product page.
    /// </summary>
    /// <param name="command">The approval command.</param>
    /// <returns>The result of the approval operation.</returns>
    Task<ApprovePhotoResult> ApprovePhotoAsync(ApprovePhotoCommand command);

    /// <summary>
    /// Removes a photo, marking it as not visible on the product page.
    /// The seller should be notified after this operation.
    /// </summary>
    /// <param name="command">The removal command.</param>
    /// <returns>The result of the removal operation.</returns>
    Task<RemovePhotoResult> RemovePhotoAsync(RemovePhotoCommand command);

    /// <summary>
    /// Gets the count of photos pending moderation.
    /// </summary>
    /// <returns>The count of pending photos.</returns>
    Task<int> GetPendingPhotoCountAsync();
}
