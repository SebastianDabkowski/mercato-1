namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a record of a status change in a return/complaint case.
/// </summary>
public class CaseStatusHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for this status history entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the return request ID this history entry belongs to.
    /// </summary>
    public Guid ReturnRequestId { get; set; }

    /// <summary>
    /// Gets or sets the previous status before the change.
    /// </summary>
    public ReturnStatus OldStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status after the change.
    /// </summary>
    public ReturnStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the user ID who made the status change.
    /// </summary>
    public string? ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the role of the user who made the change (Buyer, Seller, Admin, System).
    /// </summary>
    public string? ChangedByRole { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the status change.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the status was changed.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent return request.
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;
}
