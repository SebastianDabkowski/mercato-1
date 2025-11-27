namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents the status of a store for visibility and access control.
/// </summary>
public enum StoreStatus
{
    /// <summary>
    /// Store is awaiting verification and not visible to the public.
    /// </summary>
    PendingVerification = 0,

    /// <summary>
    /// Store is fully active and visible to the public.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Store is active with limited functionality but still visible to the public.
    /// </summary>
    LimitedActive = 2,

    /// <summary>
    /// Store is suspended and not visible to the public.
    /// </summary>
    Suspended = 3
}
