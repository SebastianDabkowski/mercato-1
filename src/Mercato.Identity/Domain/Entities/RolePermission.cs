namespace Mercato.Identity.Domain.Entities;

/// <summary>
/// Represents the mapping between a role and a permission.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Gets or sets the unique identifier for this role-permission mapping.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the role name (e.g., "Buyer", "Seller", "Admin").
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission ID.
    /// </summary>
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID who granted this permission.
    /// </summary>
    public string GrantedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this permission was granted.
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}
