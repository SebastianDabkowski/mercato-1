using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for seller login operations.
/// </summary>
public interface ISellerLoginService
{
    /// <summary>
    /// Authenticates a seller with the specified email and password.
    /// </summary>
    /// <param name="command">The login command containing email and password.</param>
    /// <returns>The result of the login attempt.</returns>
    Task<LoginSellerResult> LoginAsync(LoginSellerCommand command);
}
