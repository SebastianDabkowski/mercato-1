using Mercato.Product.Domain.Entities;

namespace Mercato.Web.Pages.Admin.Photos;

/// <summary>
/// Helper methods for photo moderation display.
/// </summary>
public static class PhotoModerationDisplayHelpers
{
    /// <summary>
    /// Gets the CSS class for a photo moderation status badge.
    /// </summary>
    /// <param name="status">The photo moderation status.</param>
    /// <returns>The CSS class name.</returns>
    public static string GetModerationStatusBadgeClass(PhotoModerationStatus status) => status switch
    {
        PhotoModerationStatus.PendingReview => "bg-warning text-dark",
        PhotoModerationStatus.Approved => "bg-success",
        PhotoModerationStatus.Removed => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the display text for a photo moderation status.
    /// </summary>
    /// <param name="status">The photo moderation status.</param>
    /// <returns>The display text.</returns>
    public static string GetModerationStatusDisplayText(PhotoModerationStatus status) => status switch
    {
        PhotoModerationStatus.PendingReview => "Pending Review",
        PhotoModerationStatus.Approved => "Approved",
        PhotoModerationStatus.Removed => "Removed",
        _ => "Unknown"
    };

    /// <summary>
    /// Formats file size to human-readable format.
    /// </summary>
    /// <param name="bytes">The file size in bytes.</param>
    /// <returns>Human-readable file size.</returns>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
