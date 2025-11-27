namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for accepting a store user invitation.
/// </summary>
public class AcceptStoreUserInvitationCommand
{
    /// <summary>
    /// Gets or sets the invitation token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID of the user accepting the invitation.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user accepting the invitation.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
