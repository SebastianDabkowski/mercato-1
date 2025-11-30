using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Domain.Interfaces;

/// <summary>
/// Repository interface for photo moderation data access operations.
/// </summary>
public interface IPhotoModerationRepository
{
    /// <summary>
    /// Gets photos pending review with optional filtering and pagination.
    /// </summary>
    /// <param name="storeId">Optional filter by store ID.</param>
    /// <param name="flaggedOnly">If true, only returns flagged photos.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take for pagination.</param>
    /// <returns>A tuple containing the list of photos and total count.</returns>
    Task<(IReadOnlyList<ProductImage> Photos, int TotalCount)> GetPendingPhotosAsync(
        Guid? storeId = null,
        bool flaggedOnly = false,
        int skip = 0,
        int take = 20);

    /// <summary>
    /// Gets a product image by its unique identifier with product details.
    /// </summary>
    /// <param name="imageId">The image ID.</param>
    /// <returns>The product image if found; otherwise, null.</returns>
    Task<ProductImage?> GetPhotoByIdAsync(Guid imageId);

    /// <summary>
    /// Updates the moderation status of a photo.
    /// </summary>
    /// <param name="image">The product image to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdatePhotoModerationStatusAsync(ProductImage image);

    /// <summary>
    /// Adds a photo moderation decision to the audit history.
    /// </summary>
    /// <param name="decision">The moderation decision to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddModerationDecisionAsync(PhotoModerationDecision decision);

    /// <summary>
    /// Gets the moderation history for a specific photo.
    /// </summary>
    /// <param name="imageId">The image ID.</param>
    /// <returns>A list of moderation decisions ordered by creation date descending.</returns>
    Task<IReadOnlyList<PhotoModerationDecision>> GetModerationHistoryAsync(Guid imageId);

    /// <summary>
    /// Flags a photo for review.
    /// </summary>
    /// <param name="imageId">The image ID.</param>
    /// <param name="reason">The reason for flagging.</param>
    /// <returns>True if the photo was flagged successfully; otherwise, false.</returns>
    Task<bool> FlagPhotoAsync(Guid imageId, string reason);

    /// <summary>
    /// Gets the count of photos pending review.
    /// </summary>
    /// <returns>The number of photos pending review.</returns>
    Task<int> GetPendingPhotoCountAsync();
}
