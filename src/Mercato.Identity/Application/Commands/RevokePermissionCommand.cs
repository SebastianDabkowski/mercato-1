namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Command to revoke a permission from a role.
/// </summary>
public class RevokePermissionCommand
{
    /// <summary>
    /// Gets or sets the role name to revoke the permission from.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission ID to revoke.
    /// </summary>
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID of the admin performing the revocation.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;
}
