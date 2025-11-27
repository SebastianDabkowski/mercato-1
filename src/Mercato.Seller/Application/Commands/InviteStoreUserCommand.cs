using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for inviting a new internal user to a store.
/// </summary>
public class InviteStoreUserCommand
{
    /// <summary>
    /// Gets or sets the store ID to invite the user to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user to invite.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role to assign to the invited user.
    /// </summary>
    public StoreRole Role { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the person sending the invitation.
    /// </summary>
    public string InvitedBy { get; set; } = string.Empty;
}
