namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Data transfer object containing user account information for admin management.
/// </summary>
public class UserAccountInfo
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

    /// <summary>
    /// Gets or sets the user's account status.
    /// </summary>
    public UserAccountStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
