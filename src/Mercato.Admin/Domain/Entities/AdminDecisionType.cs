namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents the type of decision an admin can make on an escalated case.
/// </summary>
public enum AdminDecisionType
{
    /// <summary>
    /// Override the seller's decision and approve the return/refund.
    /// </summary>
    OverrideSellerDecision,

    /// <summary>
    /// Enforce a specific refund amount.
    /// </summary>
    EnforceRefund,

    /// <summary>
    /// Close the case without taking any action.
    /// </summary>
    CloseWithoutAction,

    /// <summary>
    /// Approve the return request.
    /// </summary>
    ApproveReturn,

    /// <summary>
    /// Reject the return request.
    /// </summary>
    RejectReturn
}
