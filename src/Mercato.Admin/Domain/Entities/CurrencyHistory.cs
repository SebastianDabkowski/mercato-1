namespace Mercato.Admin.Domain.Entities;

/// <summary>
/// Represents a historical record of changes made to a currency configuration.
/// This entity provides an audit trail for currency modifications.
/// </summary>
public class CurrencyHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for this history record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the currency that was changed.
    /// </summary>
    public Guid CurrencyId { get; set; }

    /// <summary>
    /// Gets or sets the type of change made: "Created", "Updated", "Enabled", "Disabled", "SetAsBase".
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON representation of the previous values before the change.
    /// Null for "Created" change type.
    /// </summary>
    public string? PreviousValues { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of the new values after the change.
    /// </summary>
    public string NewValues { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the change was made.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID who made the change.
    /// </summary>
    public string ChangedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user who made the change.
    /// </summary>
    public string? ChangedByUserEmail { get; set; }

    /// <summary>
    /// Gets or sets the optional reason for the change.
    /// </summary>
    public string? Reason { get; set; }
}
