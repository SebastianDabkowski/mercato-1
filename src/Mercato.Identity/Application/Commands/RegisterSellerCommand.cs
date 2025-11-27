using System.ComponentModel.DataAnnotations;

namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Command representing the data required to register a new seller.
/// </summary>
public class RegisterSellerCommand
{
    /// <summary>
    /// Gets or sets the email address for the new seller account.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for the new seller account.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password confirmation.
    /// </summary>
    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

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
    /// Gets or sets the contact phone number.
    /// </summary>
    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact person name.
    /// </summary>
    [Required(ErrorMessage = "Contact person name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Contact person name must be between 2 and 100 characters.")]
    public string ContactName { get; set; } = string.Empty;
}
