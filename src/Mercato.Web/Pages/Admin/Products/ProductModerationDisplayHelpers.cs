using Mercato.Product.Domain.Entities;

namespace Mercato.Web.Pages.Admin.Products;

/// <summary>
/// Helper methods for displaying product moderation information.
/// </summary>
public static class ProductModerationDisplayHelpers
{
    /// <summary>
    /// Gets the CSS class for a product moderation status badge.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetModerationStatusBadgeClass(ProductModerationStatus status) => status switch
    {
        ProductModerationStatus.NotSubmitted => "bg-secondary",
        ProductModerationStatus.PendingReview => "bg-warning text-dark",
        ProductModerationStatus.Approved => "bg-success",
        ProductModerationStatus.Rejected => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a product moderation status.
    /// </summary>
    /// <param name="status">The moderation status.</param>
    /// <returns>The display text.</returns>
    public static string GetModerationStatusDisplayText(ProductModerationStatus status) => status switch
    {
        ProductModerationStatus.NotSubmitted => "Not Submitted",
        ProductModerationStatus.PendingReview => "Pending Review",
        ProductModerationStatus.Approved => "Approved",
        ProductModerationStatus.Rejected => "Rejected",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the CSS class for a product status badge.
    /// </summary>
    /// <param name="status">The product status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetProductStatusBadgeClass(ProductStatus status) => status switch
    {
        ProductStatus.Draft => "bg-secondary",
        ProductStatus.Active => "bg-success",
        ProductStatus.Inactive => "bg-warning text-dark",
        ProductStatus.OutOfStock => "bg-info",
        ProductStatus.Archived => "bg-dark",
        ProductStatus.Suspended => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a product status.
    /// </summary>
    /// <param name="status">The product status.</param>
    /// <returns>The display text.</returns>
    public static string GetProductStatusDisplayText(ProductStatus status) => status switch
    {
        ProductStatus.Draft => "Draft",
        ProductStatus.Active => "Active",
        ProductStatus.Inactive => "Inactive",
        ProductStatus.OutOfStock => "Out of Stock",
        ProductStatus.Archived => "Archived",
        ProductStatus.Suspended => "Suspended",
        _ => "Unknown"
    };
}
