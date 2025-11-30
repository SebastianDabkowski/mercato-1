using Mercato.Identity.Domain.Entities;
using Mercato.Identity.Domain.Interfaces;

namespace Mercato.Identity.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of role-permission repository.
/// Provides default role-permission mappings for the marketplace platform.
/// </summary>
public class RolePermissionRepository : IRolePermissionRepository
{
    private static readonly List<RolePermission> _rolePermissions = InitializeDefaultRolePermissions();

    private static List<RolePermission> InitializeDefaultRolePermissions()
    {
        var systemUser = "system";
        var now = DateTime.UtcNow;

        return
        [
            // Admin Role - Full access
            new RolePermission { Id = 1, RoleName = "Admin", PermissionId = "admin.users.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 2, RoleName = "Admin", PermissionId = "admin.users.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 3, RoleName = "Admin", PermissionId = "admin.users.roles", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 4, RoleName = "Admin", PermissionId = "admin.products.moderate", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 5, RoleName = "Admin", PermissionId = "admin.reviews.moderate", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 6, RoleName = "Admin", PermissionId = "admin.orders.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 7, RoleName = "Admin", PermissionId = "admin.reports.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 8, RoleName = "Admin", PermissionId = "admin.settings.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 9, RoleName = "Admin", PermissionId = "admin.categories.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 10, RoleName = "Admin", PermissionId = "admin.commissions.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 11, RoleName = "Admin", PermissionId = "admin.rbac.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 12, RoleName = "Admin", PermissionId = "admin.rbac.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 13, RoleName = "Admin", PermissionId = "support.cases.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 14, RoleName = "Admin", PermissionId = "support.cases.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 15, RoleName = "Admin", PermissionId = "compliance.audit.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 16, RoleName = "Admin", PermissionId = "compliance.data.export", GrantedBy = systemUser, GrantedAt = now },

            // Seller Role - Seller-specific permissions
            new RolePermission { Id = 17, RoleName = "Seller", PermissionId = "seller.dashboard.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 18, RoleName = "Seller", PermissionId = "seller.products.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 19, RoleName = "Seller", PermissionId = "seller.orders.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 20, RoleName = "Seller", PermissionId = "seller.orders.fulfill", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 21, RoleName = "Seller", PermissionId = "seller.returns.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 22, RoleName = "Seller", PermissionId = "seller.settings.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 23, RoleName = "Seller", PermissionId = "seller.reports.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 24, RoleName = "Seller", PermissionId = "product.catalog.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 25, RoleName = "Seller", PermissionId = "product.details.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 26, RoleName = "Seller", PermissionId = "product.search", GrantedBy = systemUser, GrantedAt = now },

            // Buyer Role - Buyer-specific permissions
            new RolePermission { Id = 27, RoleName = "Buyer", PermissionId = "buyer.profile.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 28, RoleName = "Buyer", PermissionId = "buyer.addresses.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 29, RoleName = "Buyer", PermissionId = "buyer.orders.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 30, RoleName = "Buyer", PermissionId = "buyer.orders.place", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 31, RoleName = "Buyer", PermissionId = "buyer.returns.request", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 32, RoleName = "Buyer", PermissionId = "buyer.reviews.submit", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 33, RoleName = "Buyer", PermissionId = "product.catalog.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 34, RoleName = "Buyer", PermissionId = "product.details.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 35, RoleName = "Buyer", PermissionId = "product.search", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 36, RoleName = "Buyer", PermissionId = "cart.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 37, RoleName = "Buyer", PermissionId = "cart.checkout", GrantedBy = systemUser, GrantedAt = now },

            // Support Role - Support staff permissions
            new RolePermission { Id = 38, RoleName = "Support", PermissionId = "support.cases.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 39, RoleName = "Support", PermissionId = "support.cases.manage", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 40, RoleName = "Support", PermissionId = "admin.orders.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 41, RoleName = "Support", PermissionId = "admin.users.view", GrantedBy = systemUser, GrantedAt = now },

            // Compliance Role - Compliance officer permissions
            new RolePermission { Id = 42, RoleName = "Compliance", PermissionId = "compliance.audit.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 43, RoleName = "Compliance", PermissionId = "compliance.data.export", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 44, RoleName = "Compliance", PermissionId = "admin.users.view", GrantedBy = systemUser, GrantedAt = now },
            new RolePermission { Id = 45, RoleName = "Compliance", PermissionId = "admin.reports.view", GrantedBy = systemUser, GrantedAt = now }
        ];
    }

    private static int _nextId = 46;

    /// <inheritdoc />
    public Task<IReadOnlyList<RolePermission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<RolePermission>>(_rolePermissions.ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RolePermission>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var rolePermissions = _rolePermissions
            .Where(rp => rp.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<RolePermission>>(rolePermissions);
    }

    /// <inheritdoc />
    public Task<bool> HasPermissionAsync(string roleName, string permissionId, CancellationToken cancellationToken = default)
    {
        var hasPermission = _rolePermissions.Any(rp =>
            rp.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase) &&
            rp.PermissionId.Equals(permissionId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(hasPermission);
    }

    /// <inheritdoc />
    public Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        // Check if already exists
        var exists = _rolePermissions.Any(rp =>
            rp.RoleName.Equals(rolePermission.RoleName, StringComparison.OrdinalIgnoreCase) &&
            rp.PermissionId.Equals(rolePermission.PermissionId, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            rolePermission.Id = _nextId++;
            _rolePermissions.Add(rolePermission);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string roleName, string permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermission = _rolePermissions.FirstOrDefault(rp =>
            rp.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase) &&
            rp.PermissionId.Equals(permissionId, StringComparison.OrdinalIgnoreCase));

        if (rolePermission != null)
        {
            _rolePermissions.Remove(rolePermission);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAllForRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        _rolePermissions.RemoveAll(rp =>
            rp.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }
}
