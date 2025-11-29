namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the status of a product review.
/// </summary>
public enum ReviewStatus
{
    /// <summary>
    /// The review is pending moderation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The review has been published and is visible publicly.
    /// </summary>
    Published = 1,

    /// <summary>
    /// The review has been hidden by moderation.
    /// </summary>
    Hidden = 2
}
