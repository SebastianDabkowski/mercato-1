namespace Mercato.Web.Pages.Admin.Photos;

/// <summary>
/// Helper methods for validating image paths to prevent security vulnerabilities.
/// </summary>
public static class ImagePathValidator
{
    /// <summary>
    /// Trusted prefixes for image paths.
    /// </summary>
    private static readonly string[] TrustedPrefixes =
    [
        "/uploads/",
        "/images/"
    ];

    /// <summary>
    /// Validates if an image path is from a trusted source.
    /// </summary>
    /// <param name="path">The image path to validate.</param>
    /// <returns>True if the path starts with a trusted prefix; otherwise, false.</returns>
    public static bool IsValidPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        foreach (var prefix in TrustedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the validated image source or returns null if invalid.
    /// </summary>
    /// <param name="path">The image path to validate.</param>
    /// <returns>The path if valid; otherwise, null.</returns>
    public static string? GetValidatedPath(string? path)
    {
        return IsValidPath(path) ? path : null;
    }
}
