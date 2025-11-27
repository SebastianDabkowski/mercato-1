using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for password reset operations.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Requests a password reset for the specified email address.
    /// </summary>
    /// <param name="command">The forgot password command.</param>
    /// <returns>The result of the password reset request.</returns>
    Task<ForgotPasswordResult> RequestPasswordResetAsync(ForgotPasswordCommand command);
}
