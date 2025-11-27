using System.ComponentModel.DataAnnotations;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for updating a store's profile.
/// </summary>
public class UpdateStoreProfileCommand
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
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store description.
    /// </summary>
    [StringLength(2000, ErrorMessage = "Store description must be at most 2000 characters.")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the store logo URL (optional).
    /// </summary>
    [StringLength(500, ErrorMessage = "Store logo URL must be at most 500 characters.")]
    [Url(ErrorMessage = "Please enter a valid URL for the logo.")]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the contact email address (optional).
    /// </summary>
    [StringLength(254, ErrorMessage = "Contact email must be at most 254 characters.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact phone number (optional).
    /// </summary>
    [StringLength(20, ErrorMessage = "Contact phone must be at most 20 characters.")]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the website URL (optional).
    /// </summary>
    [StringLength(500, ErrorMessage = "Website URL must be at most 500 characters.")]
    [Url(ErrorMessage = "Please enter a valid website URL.")]
    public string? WebsiteUrl { get; set; }
}
