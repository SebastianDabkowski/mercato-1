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

    /// <summary>
    /// Maximum weight in kilograms.
    /// </summary>
    public const decimal WeightMaxKg = 1000m;

    /// <summary>
    /// Maximum dimension (length, width, height) in centimeters.
    /// </summary>
    public const decimal DimensionMaxCm = 500m;

    /// <summary>
    /// Maximum length for shipping methods string.
    /// </summary>
    public const int ShippingMethodsMaxLength = 500;

    /// <summary>
    /// Maximum length for images JSON string.
    /// </summary>
    public const int ImagesMaxLength = 4000;

    /// <summary>
    /// Maximum number of images allowed per product.
    /// </summary>
    public const int MaxImagesCount = 10;

    /// <summary>
    /// Minimum length for category name.
    /// </summary>
    public const int CategoryNameMinLength = 2;

    /// <summary>
    /// Maximum length for category name.
    /// </summary>
    public const int CategoryNameMaxLength = 100;
}
