namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a seller rating submitted by a buyer after a seller sub-order is delivered.
/// </summary>
public class SellerRating
{
    /// <summary>
    /// Gets or sets the unique identifier for the seller rating.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID that this rating is associated with.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID that this rating is for.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the store ID (seller's store) being rated.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID who submitted the rating.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating from 1 to 5 stars.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the rating was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the rating was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the seller sub-order.
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;
}
