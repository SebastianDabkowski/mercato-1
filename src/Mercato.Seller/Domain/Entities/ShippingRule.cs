namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents a shipping rule configuration for a store.
/// </summary>
public class ShippingRule
{
    /// <summary>
    /// Gets or sets the unique identifier for the shipping rule.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID this rule belongs to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the flat shipping rate applied to all orders.
    /// </summary>
    public decimal FlatRate { get; set; }

    /// <summary>
    /// Gets or sets the minimum order subtotal for free shipping.
    /// If null, free shipping threshold is not applied.
    /// </summary>
    public decimal? FreeShippingThreshold { get; set; }

    /// <summary>
    /// Gets or sets the per-item shipping rate added on top of flat rate.
    /// </summary>
    public decimal PerItemRate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the rule was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the rule was last updated.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}
