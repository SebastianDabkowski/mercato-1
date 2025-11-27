using System.ComponentModel.DataAnnotations;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for saving verification data in the onboarding wizard.
/// </summary>
public class SaveVerificationDataCommand
{
    /// <summary>
    /// Gets or sets the seller's user ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business name.
    /// </summary>
    [Required(ErrorMessage = "Business name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Business name must be between 2 and 200 characters.")]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business address.
    /// </summary>
    [Required(ErrorMessage = "Business address is required.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Business address must be between 5 and 500 characters.")]
    public string BusinessAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax identification number.
    /// </summary>
    [Required(ErrorMessage = "Tax ID is required.")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Tax ID must be between 5 and 50 characters.")]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the business registration number (optional).
    /// </summary>
    [StringLength(50, ErrorMessage = "Business registration number must be at most 50 characters.")]
    public string? BusinessRegistrationNumber { get; set; }
}
