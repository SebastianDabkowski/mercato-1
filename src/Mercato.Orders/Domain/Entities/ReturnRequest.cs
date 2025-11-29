namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a return or complaint case initiated by a buyer for items in a seller sub-order.
/// </summary>
public class ReturnRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the return request.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable case number for display (e.g., "CASE-A1B2C3D4").
    /// </summary>
    public string CaseNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of case (Return or Complaint).
    /// </summary>
    public CaseType CaseType { get; set; } = CaseType.Return;

    /// <summary>
    /// Gets or sets the seller sub-order ID that this return request is for.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID who initiated the return request.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the return request.
    /// </summary>
    public ReturnStatus Status { get; set; } = ReturnStatus.Requested;

    /// <summary>
    /// Gets or sets the reason provided by the buyer for the return or complaint.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the return request was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the return request was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets optional notes from the seller when approving or rejecting the return.
    /// </summary>
    public string? SellerNotes { get; set; }

    /// <summary>
    /// Navigation property to the seller sub-order being returned.
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;

    /// <summary>
    /// Navigation property to the items included in this case.
    /// If empty, the case applies to all items in the sub-order.
    /// </summary>
    public ICollection<CaseItem> CaseItems { get; set; } = new List<CaseItem>();

    /// <summary>
    /// Navigation property to the messages in this case's messaging thread.
    /// </summary>
    public ICollection<CaseMessage> Messages { get; set; } = new List<CaseMessage>();

    /// <summary>
    /// Gets or sets a value indicating whether there is new activity on this case
    /// that the other party has not yet viewed.
    /// </summary>
    public bool HasNewActivity { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the last user who added activity to this case.
    /// Used to determine which party should see the "new activity" indicator.
    /// </summary>
    public string? LastActivityByUserId { get; set; }

    /// <summary>
    /// Gets a value indicating whether this case applies to specific items or the entire sub-order.
    /// </summary>
    public bool HasSelectedItems => CaseItems.Count > 0;

    /// <summary>
    /// Gets or sets the type of resolution chosen by the seller.
    /// </summary>
    public CaseResolutionType? ResolutionType { get; set; }

    /// <summary>
    /// Gets or sets the reason provided by the seller for the resolution (especially for NoRefund).
    /// </summary>
    public string? ResolutionReason { get; set; }

    /// <summary>
    /// Gets or sets the linked refund ID in the Payments module (nullable).
    /// </summary>
    public Guid? LinkedRefundId { get; set; }

    /// <summary>
    /// Gets or sets the amount refunded (for display purposes).
    /// </summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets when the case was resolved.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets when the case was escalated to admin review.
    /// </summary>
    public DateTimeOffset? EscalatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who escalated the case.
    /// </summary>
    public string? EscalatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the reason for escalation.
    /// </summary>
    public string? EscalationReason { get; set; }

    /// <summary>
    /// Gets or sets the admin's decision on the escalated case.
    /// </summary>
    public string? AdminDecision { get; set; }

    /// <summary>
    /// Gets or sets the reason for the admin's decision.
    /// </summary>
    public string? AdminDecisionReason { get; set; }

    /// <summary>
    /// Gets or sets when the admin decision was made.
    /// </summary>
    public DateTimeOffset? AdminDecisionAt { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who made the decision.
    /// </summary>
    public string? AdminDecisionByUserId { get; set; }

    /// <summary>
    /// Navigation property to the status history entries for this case.
    /// </summary>
    public ICollection<CaseStatusHistory> StatusHistory { get; set; } = new List<CaseStatusHistory>();
}
