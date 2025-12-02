using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for admin login operations.
/// </summary>
public interface IAdminLoginService
{
    /// <summary>
    /// Authenticates an admin with the specified email and password.
    /// </summary>
    /// <param name="command">The login command containing email and password.</param>
    /// <returns>The result of the login attempt.</returns>
    Task<LoginAdminResult> LoginAsync(LoginAdminCommand command);
}
