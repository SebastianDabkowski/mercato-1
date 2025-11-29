namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents the status of a seller payout.
/// </summary>
public enum PayoutStatus
{
    /// <summary>
    /// Payout is scheduled for processing.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Payout is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Payout has been successfully completed.
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Payout has failed.
    /// </summary>
    Failed = 3
}
