using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for buyer login operations.
/// </summary>
public interface IBuyerLoginService
{
    /// <summary>
    /// Authenticates a buyer with the specified email and password.
    /// </summary>
    /// <param name="command">The login command containing email and password.</param>
    /// <returns>The result of the login attempt.</returns>
    Task<LoginBuyerResult> LoginAsync(LoginBuyerCommand command);
}
