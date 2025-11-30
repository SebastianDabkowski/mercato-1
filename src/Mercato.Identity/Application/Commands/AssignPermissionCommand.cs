namespace Mercato.Identity.Application.Commands;

/// <summary>
/// Command to assign a permission to a role.
/// </summary>
public class AssignPermissionCommand
{
    /// <summary>
    /// Gets or sets the role name to assign the permission to.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission ID to assign.
    /// </summary>
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID of the admin performing the assignment.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;
}
