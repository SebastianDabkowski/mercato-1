namespace Mercato.Identity.Application.Queries;

/// <summary>
/// Represents the RBAC configuration for a single role.
/// </summary>
public class RolePermissionConfiguration
{
    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of permissions assigned to this role.
    /// </summary>
    public IReadOnlyList<PermissionInfo> Permissions { get; set; } = [];
}

/// <summary>
/// Represents permission information for display purposes.
/// </summary>
public class PermissionInfo
{
    /// <summary>
    /// Gets or sets the permission ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module this permission belongs to.
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this permission is assigned to the role.
    /// </summary>
    public bool IsAssigned { get; set; }
}
