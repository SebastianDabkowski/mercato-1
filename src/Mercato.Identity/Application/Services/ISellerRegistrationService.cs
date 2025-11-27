using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for seller registration operations.
/// </summary>
public interface ISellerRegistrationService
{
    /// <summary>
    /// Registers a new seller with the specified email, password, and business details.
    /// </summary>
    /// <param name="command">The registration command containing email, password, and business information.</param>
    /// <returns>The result of the registration attempt.</returns>
    Task<RegisterSellerResult> RegisterAsync(RegisterSellerCommand command);
}
