namespace Mercato.Product.Application.Commands;

/// <summary>
/// Represents a variant attribute definition with its possible values.
/// </summary>
public class VariantAttributeDefinition
{
    /// <summary>
    /// Gets or sets the attribute name (e.g., "Size", "Color").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the possible values for this attribute (e.g., ["Small", "Medium", "Large"]).
    /// </summary>
    public List<string> Values { get; set; } = [];
}

/// <summary>
/// Represents a variant definition with its attribute values, price, stock, and other properties.
/// </summary>
public class VariantDefinition
{
    /// <summary>
    /// Gets or sets the attribute combination (e.g., {"Size": "Medium", "Color": "Blue"}).
    /// </summary>
    public Dictionary<string, string> AttributeValues { get; set; } = [];

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

/// <summary>
/// Command for configuring variants on a product.
/// </summary>
public class ConfigureProductVariantsCommand
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID performing this action.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variant attributes with their possible values.
    /// </summary>
    public List<VariantAttributeDefinition> Attributes { get; set; } = [];

    /// <summary>
    /// Gets or sets the variant definitions.
    /// </summary>
    public List<VariantDefinition> Variants { get; set; } = [];
}
