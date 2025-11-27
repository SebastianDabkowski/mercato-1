namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents the status of an internal store user.
/// </summary>
public enum StoreUserStatus
{
    /// <summary>
    /// User has been invited but has not yet completed registration.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// User is active and can access the seller panel.
    /// </summary>
    Active = 1,

    /// <summary>
    /// User has been deactivated and cannot access the seller panel.
    /// </summary>
    Deactivated = 2
}
