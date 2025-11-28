using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for changing the workflow status of a product.
/// </summary>
public class ChangeProductStatusCommand
{
    /// <summary>
    /// Gets or sets the product ID to change status.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID performing the status change (for authorization and audit).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID that owns this product (for authorization).
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the new status to apply to the product.
    /// </summary>
    public ProductStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an admin override.
    /// Admins can bypass normal transition rules (e.g., suspend for policy violations).
    /// </summary>
    public bool IsAdminOverride { get; set; }
}
