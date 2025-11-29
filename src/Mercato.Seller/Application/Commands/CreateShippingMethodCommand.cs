using System.ComponentModel.DataAnnotations;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for creating a new shipping method for a store.
/// </summary>
public class CreateShippingMethodCommand
{
    /// <summary>
    /// Gets or sets the store ID this shipping method belongs to.
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
}
