namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents a historical record of commission calculated for a transaction.
/// </summary>
public class CommissionRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for this commission record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction ID this commission belongs to.
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the order ID associated with this commission.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID for this commission.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the original order amount for this seller.
    /// </summary>
    public decimal OrderAmount { get; set; }

    /// <summary>
    /// Gets or sets the commission rate that was applied (snapshotted at calculation time).
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Gets or sets the calculated commission amount.
    /// </summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the amount that has been refunded.
    /// </summary>
    public decimal RefundedAmount { get; set; }

    /// <summary>
    /// Gets or sets the commission amount returned due to refunds.
    /// </summary>
    public decimal RefundedCommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the net commission amount after refunds (CommissionAmount - RefundedCommissionAmount).
    /// </summary>
    public decimal NetCommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the ID of the rule that was applied (for auditing).
    /// </summary>
    public Guid? AppliedRuleId { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the rule applied.
    /// </summary>
    public string? AppliedRuleDescription { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this record was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the commission was first calculated.
    /// </summary>
    public DateTimeOffset CalculatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the last partial refund recalculation happened.
    /// </summary>
    public DateTimeOffset? LastRefundRecalculatedAt { get; set; }
}
