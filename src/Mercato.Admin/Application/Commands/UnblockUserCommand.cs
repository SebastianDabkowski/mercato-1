namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Command to unblock a user account.
/// </summary>
public class UnblockUserCommand
{
    /// <summary>
    /// Gets or sets the ID of the user to unblock.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the admin performing the unblock.
    /// </summary>
    public string AdminUserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the admin performing the unblock.
    /// </summary>
    public string AdminEmail { get; init; } = string.Empty;
}
