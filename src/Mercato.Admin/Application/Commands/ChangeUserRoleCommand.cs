namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Command for changing a user's role.
/// </summary>
public class ChangeUserRoleCommand
{
    /// <summary>
    /// Gets or sets the ID of the user whose role is being changed.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new role to assign to the user.
    /// </summary>
    public string NewRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the admin performing the role change.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;
}
