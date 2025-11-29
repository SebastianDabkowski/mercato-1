namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the type of resolution for a return/complaint case.
/// </summary>
public enum CaseResolutionType
{
    /// <summary>
    /// Full refund of all items in the case.
    /// </summary>
    FullRefund = 0,

    /// <summary>
    /// Partial refund of some amount.
    /// </summary>
    PartialRefund = 1,

    /// <summary>
    /// Product replacement (no refund, new item sent).
    /// </summary>
    Replacement = 2,

    /// <summary>
    /// Product repair (no refund, item fixed and returned).
    /// </summary>
    Repair = 3,

    /// <summary>
    /// No refund provided (case rejected or resolved without compensation).
    /// </summary>
    NoRefund = 4
}
