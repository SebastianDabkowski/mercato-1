namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents an individual order/transaction line item within a settlement.
/// </summary>
public class SettlementLineItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the line item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the settlement ID this line item belongs to.
    /// </summary>
    public Guid SettlementId { get; set; }

    /// <summary>
    /// Gets or sets the order ID for this line item.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order number for display purposes.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order date.
    /// </summary>
    public DateTimeOffset OrderDate { get; set; }

    /// <summary>
    /// Gets or sets the gross amount for this order.
    /// </summary>
    public decimal GrossAmount { get; set; }

    /// <summary>
    /// Gets or sets the refund amount for this order.
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets the net amount (GrossAmount - RefundAmount).
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Gets or sets the commission amount for this order.
    /// </summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets whether this is an adjustment from a previous month.
    /// </summary>
    public bool IsAdjustment { get; set; }

    /// <summary>
    /// Gets or sets the original settlement month if this is an adjustment.
    /// </summary>
    public int? OriginalMonth { get; set; }

    /// <summary>
    /// Gets or sets the original settlement year if this is an adjustment.
    /// </summary>
    public int? OriginalYear { get; set; }

    /// <summary>
    /// Gets or sets notes about the adjustment.
    /// </summary>
    public string? AdjustmentNotes { get; set; }

    /// <summary>
    /// Navigation property to the settlement.
    /// </summary>
    public Settlement? Settlement { get; set; }
}
