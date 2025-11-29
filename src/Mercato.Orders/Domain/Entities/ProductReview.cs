namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a product review submitted by a buyer after an order is delivered.
/// </summary>
public class ProductReview
{
    /// <summary>
    /// Gets or sets the unique identifier for the product review.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID that this review is associated with.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID that this review is associated with.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order item ID that this review is for.
    /// </summary>
    public Guid SellerSubOrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID being reviewed.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that sold the product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID who wrote the review.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rating from 1 to 5 stars.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Gets or sets the review text content.
    /// </summary>
    public string ReviewText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the review.
    /// </summary>
    public ReviewStatus Status { get; set; } = ReviewStatus.Published;

    /// <summary>
    /// Gets or sets the date and time when the review was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the review was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the seller sub-order item being reviewed.
    /// </summary>
    public SellerSubOrderItem SellerSubOrderItem { get; set; } = null!;

    /// <summary>
    /// Navigation property to the seller sub-order.
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;
}
