namespace Mercato.Product.Domain.Entities;

/// <summary>
/// Represents a moderation decision made by an admin on a product photo.
/// Used for maintaining audit history for dispute resolution.
/// </summary>
public class PhotoModerationDecision
{
    /// <summary>
    /// Gets or sets the unique identifier for the moderation decision.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product image ID that was moderated.
    /// </summary>
    public Guid ProductImageId { get; set; }

    /// <summary>
    /// Gets or sets the product ID that the image belongs to.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns the product.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who made the decision.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the moderation decision (Approved or Removed).
    /// </summary>
    public PhotoModerationStatus Decision { get; set; }

    /// <summary>
    /// Gets or sets the reason for the decision.
    /// Required for removals, optional for approvals.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the moderation status before this decision was made.
    /// </summary>
    public PhotoModerationStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the decision was made.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the admin who made the decision.
    /// </summary>
    public string? IpAddress { get; set; }
}
