namespace Mercato.Product.Domain;

/// <summary>
/// Validation constants for product variant fields.
/// </summary>
public static class ProductVariantValidationConstants
{
    /// <summary>
    /// Maximum length for variant attribute name.
    /// </summary>
    public const int AttributeNameMaxLength = 50;

    /// <summary>
    /// Minimum length for variant attribute name.
    /// </summary>
    public const int AttributeNameMinLength = 1;

    /// <summary>
    /// Maximum length for variant attribute value.
    /// </summary>
    public const int AttributeValueMaxLength = 100;

    /// <summary>
    /// Minimum length for variant attribute value.
    /// </summary>
    public const int AttributeValueMinLength = 1;

    /// <summary>
    /// Maximum length for attribute combination JSON string.
    /// </summary>
    public const int AttributeCombinationMaxLength = 1000;

    /// <summary>
    /// Maximum number of variant attributes per product.
    /// </summary>
    public const int MaxAttributesPerProduct = 5;

    /// <summary>
    /// Maximum number of values per variant attribute.
    /// </summary>
    public const int MaxValuesPerAttribute = 50;

    /// <summary>
    /// Maximum number of variants per product.
    /// </summary>
    public const int MaxVariantsPerProduct = 250;
}
