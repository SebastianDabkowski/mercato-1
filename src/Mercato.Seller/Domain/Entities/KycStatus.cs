namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents the status of a KYC submission.
/// </summary>
public enum KycStatus
{
    /// <summary>
    /// Submission received and awaiting review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Submission is currently being reviewed.
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// Submission has been approved.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Submission has been rejected.
    /// </summary>
    Rejected = 3
}
