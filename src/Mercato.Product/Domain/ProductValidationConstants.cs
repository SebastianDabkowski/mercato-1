namespace Mercato.Product.Domain;

/// <summary>
/// Validation constants for product fields.
/// </summary>
public static class ProductValidationConstants
{
    /// <summary>
    /// Minimum length for product title.
    /// </summary>
    public const int TitleMinLength = 2;

    /// <summary>
    /// Maximum length for product title.
    /// </summary>
    public const int TitleMaxLength = 200;

    /// <summary>
    /// Minimum length for product category.
    /// </summary>
    public const int CategoryMinLength = 2;

    /// <summary>
    /// Maximum length for product category.
    /// </summary>
    public const int CategoryMaxLength = 100;

    /// <summary>
    /// Maximum length for product description.
    /// </summary>
    public const int DescriptionMaxLength = 2000;
}
