using System.ComponentModel.DataAnnotations;

namespace Mercato.Product.Application.Commands;

/// <summary>
/// Command for updating an existing product.
/// </summary>
public class UpdateProductCommand
{
    /// <summary>
    /// Gets or sets the product ID to update.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the seller ID performing the update (for authorization and audit).
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID that owns this product (for authorization).
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description must be at most 2000 characters.")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the available stock quantity.
    /// </summary>
    [Required(ErrorMessage = "Stock is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    [Required(ErrorMessage = "Category is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 100 characters.")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product weight in kilograms.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Weight must be between 0 and 1000 kg.")]
    public decimal? Weight { get; set; }

    /// <summary>
    /// Gets or sets the product length in centimeters.
    /// </summary>
    [Range(0, 500, ErrorMessage = "Length must be between 0 and 500 cm.")]
    public decimal? Length { get; set; }

    /// <summary>
    /// Gets or sets the product width in centimeters.
    /// </summary>
    [Range(0, 500, ErrorMessage = "Width must be between 0 and 500 cm.")]
    public decimal? Width { get; set; }

    /// <summary>
    /// Gets or sets the product height in centimeters.
    /// </summary>
    [Range(0, 500, ErrorMessage = "Height must be between 0 and 500 cm.")]
    public decimal? Height { get; set; }

    /// <summary>
    /// Gets or sets the available shipping methods for this product.
    /// </summary>
    [StringLength(500, ErrorMessage = "Shipping methods must be at most 500 characters.")]
    public string? ShippingMethods { get; set; }

    /// <summary>
    /// Gets or sets the product images as a JSON array of image URLs.
    /// </summary>
    [StringLength(4000, ErrorMessage = "Images must be at most 4000 characters.")]
    public string? Images { get; set; }
}
