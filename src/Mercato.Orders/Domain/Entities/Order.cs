namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents an order placed by a buyer containing items from one or more stores.
/// Stores a stable snapshot of prices and quantities at the time of order creation.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets or sets the unique identifier for the order.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID (linked to IdentityUser.Id).
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order number for display purposes.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.New;

    /// <summary>
    /// Gets or sets the payment transaction ID.
    /// </summary>
    public Guid? PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the payment method name used for this order.
    /// </summary>
    public string? PaymentMethodName { get; set; }

    /// <summary>
    /// Gets or sets the total amount for all items (before shipping).
    /// </summary>
    public decimal ItemsSubtotal { get; set; }

    /// <summary>
    /// Gets or sets the total shipping cost.
    /// </summary>
    public decimal ShippingTotal { get; set; }

    /// <summary>
    /// Gets or sets the total amount paid by the buyer.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the delivery address full name.
    /// </summary>
    public string DeliveryFullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery address line 1.
    /// </summary>
    public string DeliveryAddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery address line 2.
    /// </summary>
    public string? DeliveryAddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the delivery city.
    /// </summary>
    public string DeliveryCity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery state/province.
    /// </summary>
    public string? DeliveryState { get; set; }

    /// <summary>
    /// Gets or sets the delivery postal code.
    /// </summary>
    public string DeliveryPostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery country.
    /// </summary>
    public string DeliveryCountry { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery phone number.
    /// </summary>
    public string? DeliveryPhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the buyer email address for order communication.
    /// </summary>
    public string? BuyerEmail { get; set; }

    /// <summary>
    /// Gets or sets the delivery instructions provided by the buyer.
    /// </summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the order was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the order was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the order was confirmed (payment completed).
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the order was shipped.
    /// </summary>
    public DateTimeOffset? ShippedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the order was delivered.
    /// </summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the order was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the order was refunded.
    /// </summary>
    public DateTimeOffset? RefundedAt { get; set; }

    /// <summary>
    /// Navigation property to the order items.
    /// </summary>
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>
    /// Navigation property to the seller sub-orders.
    /// </summary>
    public ICollection<SellerSubOrder> SellerSubOrders { get; set; } = new List<SellerSubOrder>();
}
