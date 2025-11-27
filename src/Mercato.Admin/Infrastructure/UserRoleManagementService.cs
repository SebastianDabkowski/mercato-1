using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing user roles.
/// </summary>
public class UserRoleManagementService : IUserRoleManagementService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IRoleChangeAuditRepository _auditRepository;
    private readonly ILogger<UserRoleManagementService> _logger;

    private static readonly string[] ValidRoles = ["Buyer", "Seller", "Admin"];

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRoleManagementService"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="auditRepository">The audit repository.</param>
    /// <param name="logger">The logger.</param>
    public UserRoleManagementService(
        UserManager<IdentityUser> userManager,
        IRoleChangeAuditRepository auditRepository,
        ILogger<UserRoleManagementService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserWithRolesInfo>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserWithRolesInfo>();

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserWithRolesInfo
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList().AsReadOnly()
            });
        }

        _logger.LogInformation("Retrieved {UserCount} users with their roles.", result.Count);
        return result.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<UserWithRolesInfo?> GetUserWithRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", userId);
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new UserWithRolesInfo
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList().AsReadOnly()
        };
    }

    /// <inheritdoc/>
    public async Task<ChangeUserRoleResult> ChangeUserRoleAsync(ChangeUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate inputs
        if (string.IsNullOrEmpty(command.UserId))
        {
            return ChangeUserRoleResult.Failure("User ID is required.");
        }

        if (string.IsNullOrEmpty(command.NewRole))
        {
            return ChangeUserRoleResult.Failure("New role is required.");
        }

        if (string.IsNullOrEmpty(command.AdminUserId))
        {
            return ChangeUserRoleResult.Failure("Admin user ID is required.");
        }

        // Validate the new role is valid
        if (!ValidRoles.Contains(command.NewRole, StringComparer.OrdinalIgnoreCase))
        {
            return ChangeUserRoleResult.Failure($"Invalid role. Valid roles are: {string.Join(", ", ValidRoles)}.");
        }

        // Find the target user
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user == null)
        {
            _logger.LogWarning("Attempted to change role for non-existent user {UserId}.", command.UserId);
            return ChangeUserRoleResult.Failure("User not found.");
        }

        // Get current roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRolesString = string.Join(", ", currentRoles);

        // Check if user already has this role as their only role
        if (currentRoles.Count == 1 && currentRoles.Contains(command.NewRole, StringComparer.OrdinalIgnoreCase))
        {
            return ChangeUserRoleResult.Failure($"User already has the role '{command.NewRole}'.");
        }

        // Safety check: Admin cannot remove their own Admin role
        if (command.UserId == command.AdminUserId &&
            currentRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase) &&
            !command.NewRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Admin {AdminUserId} attempted to remove their own Admin role.", command.AdminUserId);
            return ChangeUserRoleResult.Failure("You cannot remove your own Admin role.");
        }

        // Remove all existing roles
        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to remove roles for user {UserId}: {Errors}", command.UserId, errors);
                return ChangeUserRoleResult.Failure($"Failed to remove existing roles: {errors}");
            }
        }

        // Add the new role
        var addResult = await _userManager.AddToRoleAsync(user, command.NewRole);
        if (!addResult.Succeeded)
        {
            // Attempt to restore original roles
            if (currentRoles.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, currentRoles);
            }

            var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to add role {NewRole} for user {UserId}: {Errors}", command.NewRole, command.UserId, errors);
            return ChangeUserRoleResult.Failure($"Failed to assign new role: {errors}");
        }

        // Create audit log
        var auditLog = new RoleChangeAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            UserEmail = user.Email ?? string.Empty,
            OldRole = currentRolesString,
            NewRole = command.NewRole,
            PerformedBy = command.AdminUserId,
            PerformedAt = DateTimeOffset.UtcNow,
            Details = $"Role changed from '{currentRolesString}' to '{command.NewRole}' by admin."
        };

        try
        {
            await _auditRepository.AddAsync(auditLog, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the operation - the role change succeeded
            _logger.LogError(ex, "Failed to create audit log for role change of user {UserId}.", command.UserId);
        }

        _logger.LogInformation(
            "Successfully changed role for user {UserId} ({Email}) from '{OldRole}' to '{NewRole}' by admin {AdminUserId}.",
            command.UserId,
            user.Email,
            currentRolesString,
            command.NewRole,
            command.AdminUserId);

        return ChangeUserRoleResult.Success();
    }
}
