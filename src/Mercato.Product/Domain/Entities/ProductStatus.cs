namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents the workflow status of a product in the catalog.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is in draft mode and not visible in the public catalog.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Product is active and visible in the public catalog.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Product has been deactivated and is not visible in the public catalog.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Product is out of stock.
    /// </summary>
    OutOfStock = 3,

    /// <summary>
    /// Product has been archived (soft-deleted) and is not visible anywhere.
    /// </summary>
    Archived = 4,

    /// <summary>
    /// Product has been suspended and is not available for new orders.
    /// Remains visible in order history and for reporting.
    /// </summary>
    Suspended = 5
}
