namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents a seller's reputation metrics and calculated score.
/// </summary>
public class SellerReputation
{
    /// <summary>
    /// Gets or sets the unique identifier for the seller reputation record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID this reputation belongs to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the average rating of the seller (1-5 scale).
    /// </summary>
    public decimal? AverageRating { get; set; }

    /// <summary>
    /// Gets or sets the total number of ratings received by the seller.
    /// </summary>
    public int TotalRatingsCount { get; set; }

    /// <summary>
    /// Gets or sets the percentage of orders with disputes or returns (0-100).
    /// </summary>
    public decimal? DisputeRate { get; set; }

    /// <summary>
    /// Gets or sets the percentage of on-time deliveries (0-100).
    /// </summary>
    public decimal? OnTimeShippingRate { get; set; }

    /// <summary>
    /// Gets or sets the percentage of cancelled orders (0-100).
    /// </summary>
    public decimal? CancellationRate { get; set; }

    /// <summary>
    /// Gets or sets the total number of orders used for calculation.
    /// </summary>
    public int TotalOrdersCount { get; set; }

    /// <summary>
    /// Gets or sets the calculated overall reputation score (0-100).
    /// </summary>
    public decimal? ReputationScore { get; set; }

    /// <summary>
    /// Gets or sets the reputation level based on score and order count.
    /// </summary>
    public ReputationLevel ReputationLevel { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the reputation was last calculated.
    /// </summary>
    public DateTimeOffset? LastCalculatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the reputation record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the reputation record was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}
