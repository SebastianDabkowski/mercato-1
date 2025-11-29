namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents the type of return or complaint case.
/// </summary>
public enum CaseType
{
    /// <summary>
    /// A standard return request where the buyer wants to return items.
    /// </summary>
    Return = 0,

    /// <summary>
    /// A complaint about a product issue (e.g., defective, damaged, wrong item).
    /// </summary>
    Complaint = 1
}
