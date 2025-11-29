namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents a payment transaction.
/// </summary>
public class PaymentTransaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the payment transaction.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID who made the payment.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order ID associated with this payment.
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the payment method ID used.
    /// </summary>
    public string PaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total amount of the payment.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the current status of the payment.
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the external reference ID from the payment provider.
    /// </summary>
    public string? ExternalReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the redirect URL provided by the payment provider.
    /// </summary>
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Gets or sets the callback/return URL after payment completion.
    /// </summary>
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Gets or sets any error message if the payment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payment was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the amount that was refunded (for partial or full refunds).
    /// </summary>
    public decimal RefundedAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payment was refunded.
    /// </summary>
    public DateTimeOffset? RefundedAt { get; set; }
}
