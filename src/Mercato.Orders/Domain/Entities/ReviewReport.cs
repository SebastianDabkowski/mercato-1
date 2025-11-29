namespace Mercato.Orders.Domain.Entities;

/// <summary>
/// Represents a report submitted by a buyer against a review for admin review.
/// </summary>
public class ReviewReport
{
    /// <summary>
    /// Gets or sets the unique identifier for the review report.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the review being reported.
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the buyer who submitted the report.
    /// </summary>
    public string ReporterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the report.
    /// </summary>
    public ReportReason Reason { get; set; }

    /// <summary>
    /// Gets or sets additional details about the report (optional).
    /// </summary>
    public string? AdditionalDetails { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the report was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the product review being reported.
    /// </summary>
    public ProductReview Review { get; set; } = null!;
}
