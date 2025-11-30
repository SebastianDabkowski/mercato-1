namespace Mercato.Admin.Application.Queries;

/// <summary>
/// Data transfer object containing block/reactivate history information.
/// </summary>
public class BlockHistoryInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the block record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the email of the admin who performed the block.
    /// </summary>
    public string BlockedByAdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the user was blocked.
    /// </summary>
    public DateTimeOffset BlockedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for blocking the user.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional additional details about the block reason.
    /// </summary>
    public string? ReasonDetails { get; set; }

    /// <summary>
    /// Gets or sets whether this block is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets when the user was unblocked (if applicable).
    /// </summary>
    public DateTimeOffset? UnblockedAt { get; set; }

    /// <summary>
    /// Gets or sets the email of the admin who unblocked the user (if applicable).
    /// </summary>
    public string? UnblockedByAdminEmail { get; set; }
}
