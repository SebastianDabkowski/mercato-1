using System.ComponentModel.DataAnnotations;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for saving store profile data in the onboarding wizard.
/// </summary>
public class SaveStoreProfileCommand
{
    /// <summary>
    /// Gets or sets the seller's user ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    [Required(ErrorMessage = "Store name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Store name must be between 2 and 200 characters.")]
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store description.
    /// </summary>
    [Required(ErrorMessage = "Store description is required.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Store description must be between 10 and 2000 characters.")]
    public string StoreDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store logo URL (optional).
    /// </summary>
    [StringLength(500, ErrorMessage = "Store logo URL must be at most 500 characters.")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string? StoreLogoUrl { get; set; }
}
