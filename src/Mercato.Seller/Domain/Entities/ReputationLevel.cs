namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents the reputation level of a seller based on their reputation score and order history.
/// </summary>
public enum ReputationLevel
{
    /// <summary>
    /// No data available to calculate reputation.
    /// </summary>
    Unrated = 0,

    /// <summary>
    /// New seller with fewer than 10 orders.
    /// </summary>
    New = 1,

    /// <summary>
    /// Reputation score below 60.
    /// </summary>
    Bronze = 2,

    /// <summary>
    /// Reputation score between 60 and 74.
    /// </summary>
    Silver = 3,

    /// <summary>
    /// Reputation score between 75 and 89.
    /// </summary>
    Gold = 4,

    /// <summary>
    /// Reputation score 90 or above.
    /// </summary>
    Platinum = 5
}
