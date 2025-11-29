namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents a monthly settlement for a seller summarizing orders, commissions, and payouts.
/// </summary>
public class Settlement
{
    /// <summary>
    /// Gets or sets the unique identifier for the settlement.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the seller's store ID for this settlement.
    /// </summary>
    public Guid SellerId { get; set; }

    /// <summary>
    /// Gets or sets the settlement period year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the settlement period month (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the total gross sales amount for the period.
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Gets or sets the total refunds for the period.
    /// </summary>
    public decimal TotalRefunds { get; set; }

    /// <summary>
    /// Gets or sets the net sales (GrossSales - TotalRefunds).
    /// </summary>
    public decimal NetSales { get; set; }

    /// <summary>
    /// Gets or sets the total commission charged for the period.
    /// </summary>
    public decimal TotalCommission { get; set; }

    /// <summary>
    /// Gets or sets the total adjustments from previous months.
    /// </summary>
    public decimal PreviousMonthAdjustments { get; set; }

    /// <summary>
    /// Gets or sets the net amount payable to the seller.
    /// </summary>
    public decimal NetPayable { get; set; }

    /// <summary>
    /// Gets or sets the total number of orders in this settlement.
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the current status of the settlement.
    /// </summary>
    public SettlementStatus Status { get; set; } = SettlementStatus.Draft;

    /// <summary>
    /// Gets or sets the date and time when the settlement was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the settlement was last regenerated.
    /// </summary>
    public DateTimeOffset? RegeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the settlement was finalized.
    /// </summary>
    public DateTimeOffset? FinalizedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the settlement was exported.
    /// </summary>
    public DateTimeOffset? ExportedAt { get; set; }

    /// <summary>
    /// Gets or sets the version number for audit trail.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets audit notes for regeneration history.
    /// </summary>
    public string? AuditNotes { get; set; }

    /// <summary>
    /// Gets or sets the line items (orders) in this settlement.
    /// </summary>
    public ICollection<SettlementLineItem> LineItems { get; set; } = new List<SettlementLineItem>();
}
