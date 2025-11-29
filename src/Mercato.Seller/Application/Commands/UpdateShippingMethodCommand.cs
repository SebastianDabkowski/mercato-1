using System.ComponentModel.DataAnnotations;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for updating an existing shipping method.
/// </summary>
public class UpdateShippingMethodCommand
{
    /// <summary>
    /// Gets or sets the shipping method ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID this shipping method belongs to (for authorization).
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the name of the shipping method.
    /// </summary>
    [Required(ErrorMessage = "Shipping method name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Shipping method name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the shipping method.
    /// </summary>
    [StringLength(500, ErrorMessage = "Description must be at most 500 characters.")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of ISO country codes where this shipping method is available.
    /// If null or empty, the method is available in all countries.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Available countries must be at most 1000 characters.")]
    public string? AvailableCountries { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this shipping method is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the base shipping cost (flat rate) for this method.
    /// </summary>
    [Range(0, 999999.99, ErrorMessage = "Base cost must be between 0 and 999,999.99.")]
    public decimal BaseCost { get; set; }

    /// <summary>
    /// Gets or sets the minimum estimated delivery time in business days.
    /// </summary>
    [Range(0, 365, ErrorMessage = "Minimum delivery days must be between 0 and 365.")]
    public int? EstimatedDeliveryMinDays { get; set; }

    /// <summary>
    /// Gets or sets the maximum estimated delivery time in business days.
    /// </summary>
    [Range(0, 365, ErrorMessage = "Maximum delivery days must be between 0 and 365.")]
    public int? EstimatedDeliveryMaxDays { get; set; }
}
