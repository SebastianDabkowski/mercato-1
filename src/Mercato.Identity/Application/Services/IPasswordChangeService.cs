using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for changing user passwords from account settings.
/// </summary>
public interface IPasswordChangeService
{
    /// <summary>
    /// Changes the user's password after validating the current password.
    /// </summary>
    /// <param name="command">The change password command containing user ID, current password, and new password.</param>
    /// <returns>The result of the password change operation.</returns>
    Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordCommand command);

    /// <summary>
    /// Checks if the user has a password set (for users who registered via social login).
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>True if the user has a password set, false otherwise.</returns>
    Task<bool> HasPasswordAsync(string userId);
}
