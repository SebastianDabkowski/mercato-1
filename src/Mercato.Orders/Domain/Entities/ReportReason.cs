namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the reason for reporting a review.
/// </summary>
public enum ReportReason
{
    /// <summary>
    /// The review contains abusive content.
    /// </summary>
    Abuse = 0,

    /// <summary>
    /// The review is spam or promotional content.
    /// </summary>
    Spam = 1,

    /// <summary>
    /// The review contains false or misleading information.
    /// </summary>
    FalseInformation = 2,

    /// <summary>
    /// The review is being reported for other reasons.
    /// </summary>
    Other = 3
}
