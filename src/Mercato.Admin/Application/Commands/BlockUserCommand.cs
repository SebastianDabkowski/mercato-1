using Mercato.Admin.Domain.Entities;

namespace Mercato.Admin.Application.Commands;

/// <summary>
/// Command to block a user account.
/// </summary>
public class BlockUserCommand
{
    /// <summary>
    /// Gets or sets the ID of the user to block.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the admin performing the block.
    /// </summary>
    public string AdminUserId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the admin performing the block.
    /// </summary>
    public string AdminEmail { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for blocking the user.
    /// </summary>
    public BlockReason Reason { get; init; }

    /// <summary>
    /// Gets or sets optional additional details about the block reason.
    /// </summary>
    public string? ReasonDetails { get; init; }
}
