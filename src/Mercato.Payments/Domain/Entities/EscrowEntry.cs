namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents an escrow entry tracking funds held for a specific seller within an order.
/// </summary>
public class EscrowEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for this escrow entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction ID that this escrow entry belongs to.
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the order ID associated with this escrow entry.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller's store ID for this escrow allocation.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the amount held in escrow for this seller.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the current status of this escrow entry.
    /// </summary>
    public EscrowStatus Status { get; set; } = EscrowStatus.Held;

    /// <summary>
    /// Gets or sets the date and time when this escrow entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this escrow entry was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when funds were released to the seller.
    /// </summary>
    public DateTimeOffset? ReleasedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when funds were refunded to the buyer.
    /// </summary>
    public DateTimeOffset? RefundedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this entry is eligible for automatic payout.
    /// </summary>
    public bool IsEligibleForPayout { get; set; }

    /// <summary>
    /// Gets or sets a note for auditing purposes.
    /// </summary>
    public string? AuditNote { get; set; }
}
