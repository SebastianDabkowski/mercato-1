namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents the moderation status of a product for admin review.
/// </summary>
public enum ProductModerationStatus
{
    /// <summary>
    /// Product has not been submitted for moderation review.
    /// </summary>
    NotSubmitted = 0,

    /// <summary>
    /// Product is pending admin review in the moderation queue.
    /// </summary>
    PendingReview = 1,

    /// <summary>
    /// Product has been approved by an admin and is eligible for Active status.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Product has been rejected by an admin and cannot be made active.
    /// </summary>
    Rejected = 3
}
