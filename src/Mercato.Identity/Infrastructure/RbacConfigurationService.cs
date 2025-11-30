using Mercato.Identity.Application.Commands;
using Mercato.Identity.Application.Queries;
using Mercato.Identity.Application.Services;
using Mercato.Identity.Domain.Entities;
using Mercato.Identity.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Mercato.Identity.Infrastructure;

/// <summary>
/// Service implementation for managing RBAC (Role-Based Access Control) configuration.
/// </summary>
public class RbacConfigurationService : IRbacConfigurationService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly RoleManager<IdentityRole> _roleManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RbacConfigurationService"/> class.
    /// </summary>
    /// <param name="permissionRepository">The permission repository.</param>
    /// <param name="rolePermissionRepository">The role-permission repository.</param>
    /// <param name="roleManager">The ASP.NET Core Identity role manager.</param>
    public RbacConfigurationService(
        IPermissionRepository permissionRepository,
        IRolePermissionRepository rolePermissionRepository,
        RoleManager<IdentityRole> roleManager)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _rolePermissionRepository = rolePermissionRepository ?? throw new ArgumentNullException(nameof(rolePermissionRepository));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionInfo>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        return permissions.Select(p => new PermissionInfo
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Module = p.Module
        }).ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = _roleManager.Roles.Select(r => r.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList();
        return Task.FromResult<IReadOnlyList<string>>(roles);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RolePermissionConfiguration>> GetRbacConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var roles = await GetAllRolesAsync(cancellationToken);
        var configurations = new List<RolePermissionConfiguration>();

        foreach (var roleName in roles)
        {
            var config = await GetRoleConfigurationAsync(roleName, cancellationToken);
            if (config != null)
            {
                configurations.Add(config);
            }
        }

        return configurations;
    }

    /// <inheritdoc />
    public async Task<RolePermissionConfiguration?> GetRoleConfigurationAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return null;
        }

        var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var rolePermissions = await _rolePermissionRepository.GetByRoleAsync(roleName, cancellationToken);
        var assignedPermissionIds = rolePermissions.Select(rp => rp.PermissionId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permissions = allPermissions.Select(p => new PermissionInfo
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Module = p.Module,
            IsAssigned = assignedPermissionIds.Contains(p.Id)
        }).ToList();

        return new RolePermissionConfiguration
        {
            RoleName = roleName,
            Permissions = permissions
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionInfo>> GetPermissionsByModuleAsync(string roleName, string module, CancellationToken cancellationToken = default)
    {
        var modulePermissions = await _permissionRepository.GetByModuleAsync(module, cancellationToken);
        var rolePermissions = await _rolePermissionRepository.GetByRoleAsync(roleName, cancellationToken);
        var assignedPermissionIds = rolePermissions.Select(rp => rp.PermissionId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return modulePermissions.Select(p => new PermissionInfo
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Module = p.Module,
            IsAssigned = assignedPermissionIds.Contains(p.Id)
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<AssignPermissionResult> AssignPermissionAsync(AssignPermissionCommand command, CancellationToken cancellationToken = default)
    {
        var errors = ValidateAssignPermissionCommand(command);
        if (errors.Count > 0)
        {
            return AssignPermissionResult.Failure([.. errors]);
        }

        // Verify role exists
        var role = await _roleManager.FindByNameAsync(command.RoleName);
        if (role == null)
        {
            return AssignPermissionResult.Failure($"Role '{command.RoleName}' does not exist.");
        }

        // Verify permission exists
        var permission = await _permissionRepository.GetByIdAsync(command.PermissionId, cancellationToken);
        if (permission == null)
        {
            return AssignPermissionResult.Failure($"Permission '{command.PermissionId}' does not exist.");
        }

        // Check if already assigned
        var hasPermission = await _rolePermissionRepository.HasPermissionAsync(command.RoleName, command.PermissionId, cancellationToken);
        if (hasPermission)
        {
            return AssignPermissionResult.Failure($"Permission '{command.PermissionId}' is already assigned to role '{command.RoleName}'.");
        }

        // Assign the permission
        var rolePermission = new RolePermission
        {
            RoleName = command.RoleName,
            PermissionId = command.PermissionId,
            GrantedBy = command.AdminUserId,
            GrantedAt = DateTime.UtcNow
        };

        await _rolePermissionRepository.AddAsync(rolePermission, cancellationToken);

        return AssignPermissionResult.Success();
    }

    /// <inheritdoc />
    public async Task<RevokePermissionResult> RevokePermissionAsync(RevokePermissionCommand command, CancellationToken cancellationToken = default)
    {
        var errors = ValidateRevokePermissionCommand(command);
        if (errors.Count > 0)
        {
            return RevokePermissionResult.Failure([.. errors]);
        }

        // Verify role exists
        var role = await _roleManager.FindByNameAsync(command.RoleName);
        if (role == null)
        {
            return RevokePermissionResult.Failure($"Role '{command.RoleName}' does not exist.");
        }

        // Verify permission exists
        var permission = await _permissionRepository.GetByIdAsync(command.PermissionId, cancellationToken);
        if (permission == null)
        {
            return RevokePermissionResult.Failure($"Permission '{command.PermissionId}' does not exist.");
        }

        // Check if actually assigned
        var hasPermission = await _rolePermissionRepository.HasPermissionAsync(command.RoleName, command.PermissionId, cancellationToken);
        if (!hasPermission)
        {
            return RevokePermissionResult.Failure($"Permission '{command.PermissionId}' is not assigned to role '{command.RoleName}'.");
        }

        // Revoke the permission
        await _rolePermissionRepository.RemoveAsync(command.RoleName, command.PermissionId, cancellationToken);

        return RevokePermissionResult.Success();
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(string roleName, string permissionId, CancellationToken cancellationToken = default)
    {
        return await _rolePermissionRepository.HasPermissionAsync(roleName, permissionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        return permissions.Select(p => p.Module).Distinct().OrderBy(m => m).ToList();
    }

    private static List<string> ValidateAssignPermissionCommand(AssignPermissionCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.RoleName))
        {
            errors.Add("Role name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.PermissionId))
        {
            errors.Add("Permission ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateRevokePermissionCommand(RevokePermissionCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.RoleName))
        {
            errors.Add("Role name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.PermissionId))
        {
            errors.Add("Permission ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        return errors;
    }
}
