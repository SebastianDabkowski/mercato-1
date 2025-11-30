namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents the moderation status of a product photo for admin review.
/// </summary>
public enum PhotoModerationStatus
{
    /// <summary>
    /// Photo is pending admin review in the moderation queue.
    /// This is the default status when a photo is uploaded or flagged.
    /// </summary>
    PendingReview = 0,

    /// <summary>
    /// Photo has been approved by an admin and is visible on the product page.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Photo has been removed by an admin and is no longer visible on the product page.
    /// The photo is archived for legal retention purposes.
    /// </summary>
    Removed = 2
}
