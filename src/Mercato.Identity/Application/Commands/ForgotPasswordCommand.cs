using System.ComponentModel.DataAnnotations;

namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Command for requesting a password reset.
/// </summary>
public class ForgotPasswordCommand
{
    /// <summary>
    /// Gets or sets the email address for password reset.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;
}
