using Mercato.Orders.Domain.Entities;

namespace Mercato.Web.Pages.Admin.Reviews;

/// <summary>
/// Provides shared helper methods for displaying review information in admin pages.
/// </summary>
public static class ReviewDisplayHelpers
{
    /// <summary>
    /// Gets the CSS class for a status badge based on the review status.
    /// </summary>
    /// <param name="status">The review status.</param>
    /// <returns>The CSS class for the badge.</returns>
    public static string GetStatusBadgeClass(ReviewStatus status)
    {
        return status switch
        {
            ReviewStatus.Published => "bg-success",
            ReviewStatus.Pending => "bg-warning text-dark",
            ReviewStatus.Hidden => "bg-secondary",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Gets the display text for a review status.
    /// </summary>
    /// <param name="status">The review status.</param>
    /// <returns>The display text.</returns>
    public static string GetStatusDisplayText(ReviewStatus status)
    {
        return status switch
        {
            ReviewStatus.Published => "Published",
            ReviewStatus.Pending => "Pending",
            ReviewStatus.Hidden => "Hidden",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Gets a star rating display as HTML.
    /// </summary>
    /// <param name="rating">The rating from 1 to 5.</param>
    /// <returns>The star display string.</returns>
    public static string GetStarRating(int rating)
    {
        // Clamp rating to valid range
        var clampedRating = Math.Clamp(rating, 0, 5);
        var filled = new string('★', clampedRating);
        var empty = new string('☆', 5 - clampedRating);
        return filled + empty;
    }
}
