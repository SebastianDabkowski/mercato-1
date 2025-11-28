namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for updating a single product variant.
/// </summary>
public class UpdateProductVariantCommand
{
    /// <summary>
    /// Gets or sets the variant ID.
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID performing this action.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SKU for this variant.
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the price for this variant. If null, the product's base price is used.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Gets or sets the stock for this variant.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the images for this variant as a JSON array of URLs.
    /// </summary>
    public string? Images { get; set; }

    /// <summary>
    /// Gets or sets whether this variant is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
