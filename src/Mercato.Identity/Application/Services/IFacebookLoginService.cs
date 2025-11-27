using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for Facebook OAuth login operations for buyers.
/// </summary>
public interface IFacebookLoginService
{
    /// <summary>
    /// Processes a Facebook login callback and creates or retrieves the buyer user.
    /// </summary>
    /// <param name="email">The email address from Facebook.</param>
    /// <param name="facebookId">The unique Facebook user ID.</param>
    /// <param name="name">The user's display name from Facebook.</param>
    /// <returns>The result of the Facebook login attempt.</returns>
    Task<FacebookLoginResult> ProcessFacebookLoginAsync(string email, string facebookId, string? name);
}
