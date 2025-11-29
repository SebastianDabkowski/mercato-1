namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents a payout to a seller aggregating eligible escrow balances.
/// </summary>
public class Payout
{
    /// <summary>
    /// Gets or sets the unique identifier for the payout.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller's store ID receiving the payout.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the total amount to be paid out.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the current status of the payout.
    /// </summary>
    public PayoutStatus Status { get; set; } = PayoutStatus.Scheduled;

    /// <summary>
    /// Gets or sets the payout schedule frequency.
    /// </summary>
    public PayoutScheduleFrequency ScheduleFrequency { get; set; } = PayoutScheduleFrequency.Weekly;

    /// <summary>
    /// Gets or sets the scheduled date for the payout.
    /// </summary>
    public DateTimeOffset ScheduledAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout started processing.
    /// </summary>
    public DateTimeOffset? ProcessingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout was completed (paid or failed).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the error reference for failed payouts.
    /// </summary>
    public string? ErrorReference { get; set; }

    /// <summary>
    /// Gets or sets the error message for failed payouts.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts for this payout.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the batch ID for grouped payouts.
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the external payment provider reference.
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Gets or sets a note for auditing purposes.
    /// </summary>
    public string? AuditNote { get; set; }

    /// <summary>
    /// Gets or sets the escrow entry IDs included in this payout.
    /// </summary>
    public string? EscrowEntryIds { get; set; }
}
