namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents a refund transaction for tracking full and partial refunds.
/// </summary>
public class Refund
{
    /// <summary>
    /// Gets or sets the unique identifier for this refund.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction ID this refund is associated with.
    /// </summary>
    public Guid PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the order ID associated with this refund.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID for seller-specific refunds.
    /// Null for full order refunds across all sellers.
    /// </summary>
    public Guid? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the type of refund.
    /// </summary>
    public RefundType Type { get; set; }

    /// <summary>
    /// Gets or sets the current status of the refund.
    /// </summary>
    public RefundStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external reference ID from the payment provider.
    /// </summary>
    public string? ExternalReferenceId { get; set; }

    /// <summary>
    /// Gets or sets any error message if the refund failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who initiated the refund.
    /// </summary>
    public string InitiatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the user who initiated the refund (e.g., "Admin", "Seller").
    /// </summary>
    public string InitiatedByRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this refund was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this refund was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the refund was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the commission amount refunded (for auditing).
    /// </summary>
    public decimal CommissionRefunded { get; set; }

    /// <summary>
    /// Gets or sets the escrow amount refunded (for auditing).
    /// </summary>
    public decimal EscrowRefunded { get; set; }

    /// <summary>
    /// Gets or sets an audit note for tracking refund history.
    /// </summary>
    public string? AuditNote { get; set; }
}
