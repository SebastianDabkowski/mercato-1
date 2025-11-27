using System.ComponentModel.DataAnnotations;

namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Command for resetting a user's password using a secure token.
/// </summary>
public class ResetPasswordCommand
{
    /// <summary>
    /// Gets or sets the email address of the user resetting their password.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password reset token.
    /// </summary>
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirmation of the new password.
    /// </summary>
    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
