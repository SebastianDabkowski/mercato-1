namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for checking if a user is blocked.
/// </summary>
public interface IUserBlockCheckService
{
    /// <summary>
    /// Checks if a user is currently blocked.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user is blocked; otherwise, false.</returns>
    Task<bool> IsUserBlockedAsync(string userId, CancellationToken cancellationToken = default);
}
