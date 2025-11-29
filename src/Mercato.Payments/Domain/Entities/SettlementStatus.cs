namespace Mercato.Payments.Domain.Entities;

/// <summary>
/// Represents the status of a settlement.
/// </summary>
public enum SettlementStatus
{
    /// <summary>
    /// Settlement is in draft, can be regenerated.
    /// </summary>
    Draft,

    /// <summary>
    /// Settlement has been finalized and approved.
    /// </summary>
    Finalized,

    /// <summary>
    /// Settlement has been exported for payment.
    /// </summary>
    Exported
}
