using System.ComponentModel.DataAnnotations;

namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Command representing the data required to log in a seller.
/// </summary>
public class LoginSellerCommand
{
    /// <summary>
    /// Gets or sets the email address for the seller account.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for the seller account.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the login session should be persistent.
    /// </summary>
    public bool RememberMe { get; set; }
}
