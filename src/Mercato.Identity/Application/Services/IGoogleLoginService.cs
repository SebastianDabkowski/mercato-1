using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for Google OAuth login operations for buyers.
/// </summary>
public interface IGoogleLoginService
{
    /// <summary>
    /// Processes a Google login callback and creates or retrieves the buyer user.
    /// </summary>
    /// <param name="email">The email address from Google.</param>
    /// <param name="googleId">The unique Google user ID.</param>
    /// <param name="name">The user's display name from Google.</param>
    /// <returns>The result of the Google login attempt.</returns>
    Task<GoogleLoginResult> ProcessGoogleLoginAsync(string email, string googleId, string? name);
}
