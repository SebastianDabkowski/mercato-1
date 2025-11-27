namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Defines the roles available for internal store users.
/// This determines what permissions a user has within the seller panel.
/// </summary>
public enum StoreRole
{
    /// <summary>
    /// Full access to all store functionality including user management.
    /// </summary>
    StoreOwner = 0,

    /// <summary>
    /// Access to product and catalog management functionality.
    /// </summary>
    CatalogManager = 1,

    /// <summary>
    /// Access to order processing and fulfillment functionality.
    /// </summary>
    OrderManager = 2,

    /// <summary>
    /// Read-only access to reports and accounting data.
    /// </summary>
    ReadOnly = 3
}
