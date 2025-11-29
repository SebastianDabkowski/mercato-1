namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the sorting options for product reviews.
/// </summary>
public enum ReviewSortOption
{
    /// <summary>
    /// Sort by newest reviews first (descending by creation date).
    /// </summary>
    Newest,

    /// <summary>
    /// Sort by highest rating first (descending by rating).
    /// </summary>
    HighestRating,

    /// <summary>
    /// Sort by lowest rating first (ascending by rating).
    /// </summary>
    LowestRating
}
