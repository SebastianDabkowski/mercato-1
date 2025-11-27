namespace Mercato.Seller.Domain.Entities;

/// <summary>
/// Represents an internal user associated with a store.
/// This entity links IdentityUser accounts to stores with specific roles.
/// </summary>
public class StoreUser
{
    /// <summary>
    /// Gets or sets the unique identifier for the store user record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID this user belongs to.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the user ID (linked to IdentityUser.Id).
    /// Null if the invitation is still pending.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role assigned to this user within the store.
    /// </summary>
    public StoreRole Role { get; set; }

    /// <summary>
    /// Gets or sets the current status of the store user.
    /// </summary>
    public StoreUserStatus Status { get; set; } = StoreUserStatus.Pending;

    /// <summary>
    /// Gets or sets the invitation token for pending users.
    /// </summary>
    public string? InvitationToken { get; set; }

    /// <summary>
    /// Gets or sets when the invitation expires.
    /// </summary>
    public DateTimeOffset? InvitationExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was invited.
    /// </summary>
    public DateTimeOffset InvitedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID of who invited this user.
    /// </summary>
    public string InvitedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the user accepted the invitation.
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was deactivated.
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID of who deactivated this user.
    /// </summary>
    public string? DeactivatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the role was last changed.
    /// </summary>
    public DateTimeOffset? RoleChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID of who last changed this user's role.
    /// </summary>
    public string? RoleChangedBy { get; set; }

    /// <summary>
    /// Navigation property to the associated store.
    /// </summary>
    public Store? Store { get; set; }
}
