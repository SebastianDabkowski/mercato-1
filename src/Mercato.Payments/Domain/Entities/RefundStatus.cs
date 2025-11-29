namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents the status of a refund transaction.
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Refund is pending and waiting to be processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Refund is being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Refund has been completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Refund has failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Refund was cancelled before completion.
    /// </summary>
    Cancelled = 4
}
