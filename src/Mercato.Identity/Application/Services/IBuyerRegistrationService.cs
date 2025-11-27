using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for buyer registration operations.
/// </summary>
public interface IBuyerRegistrationService
{
    /// <summary>
    /// Registers a new buyer with the specified email and password.
    /// </summary>
    /// <param name="command">The registration command containing email and password.</param>
    /// <returns>The result of the registration attempt.</returns>
    Task<RegisterBuyerResult> RegisterAsync(RegisterBuyerCommand command);
}
