namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the status of a return request throughout its lifecycle.
/// </summary>
public enum ReturnStatus
{
    /// <summary>
    /// The return request has been submitted by the buyer and is awaiting review.
    /// </summary>
    Requested = 0,

    /// <summary>
    /// The return request is being reviewed by the seller.
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// The return request has been approved by the seller.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// The return request has been rejected by the seller.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// The return process has been completed.
    /// </summary>
    Completed = 4
}
