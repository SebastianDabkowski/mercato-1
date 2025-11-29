namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command containing the metrics needed to calculate a seller's reputation.
/// </summary>
public class CalculateReputationCommand
{
    /// <summary>
    /// Gets or sets the average rating of the seller (1-5 scale).
    /// </summary>
    public decimal? AverageRating { get; set; }

    /// <summary>
    /// Gets or sets the total number of ratings received.
    /// </summary>
    public int TotalRatingsCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of orders.
    /// </summary>
    public int TotalOrdersCount { get; set; }

    /// <summary>
    /// Gets or sets the number of delivered orders.
    /// </summary>
    public int DeliveredOrdersCount { get; set; }

    /// <summary>
    /// Gets or sets the number of on-time deliveries.
    /// </summary>
    public int OnTimeDeliveriesCount { get; set; }

    /// <summary>
    /// Gets or sets the number of cancelled orders.
    /// </summary>
    public int CancelledOrdersCount { get; set; }

    /// <summary>
    /// Gets or sets the number of disputed orders.
    /// </summary>
    public int DisputedOrdersCount { get; set; }
}
