namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Data transfer object containing user information with their roles.
/// </summary>
public class UserWithRolesInfo
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of roles assigned to the user.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = [];
}
