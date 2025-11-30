using Mercato.Identity.Domain.Entities;
using Mercato.Identity.Domain.Interfaces;

namespace Mercato.Identity.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of permission repository.
/// Provides default permissions for the marketplace platform.
/// </summary>
public class PermissionRepository : IPermissionRepository
{
    private static readonly List<Permission> _permissions = InitializeDefaultPermissions();

    private static List<Permission> InitializeDefaultPermissions()
    {
        return
        [
            // Admin Module Permissions
            new Permission { Id = "admin.users.view", Name = "View Users", Description = "View user accounts and details", Module = "Admin" },
            new Permission { Id = "admin.users.manage", Name = "Manage Users", Description = "Block, unblock, and manage user accounts", Module = "Admin" },
            new Permission { Id = "admin.users.roles", Name = "Manage User Roles", Description = "Change user roles", Module = "Admin" },
            new Permission { Id = "admin.products.moderate", Name = "Moderate Products", Description = "Review and moderate product listings", Module = "Admin" },
            new Permission { Id = "admin.reviews.moderate", Name = "Moderate Reviews", Description = "Review and moderate customer reviews", Module = "Admin" },
            new Permission { Id = "admin.orders.view", Name = "View All Orders", Description = "View all orders in the system", Module = "Admin" },
            new Permission { Id = "admin.reports.view", Name = "View Reports", Description = "Access platform analytics and reports", Module = "Admin" },
            new Permission { Id = "admin.settings.manage", Name = "Manage Settings", Description = "Configure platform settings", Module = "Admin" },
            new Permission { Id = "admin.categories.manage", Name = "Manage Categories", Description = "Create and manage product categories", Module = "Admin" },
            new Permission { Id = "admin.commissions.manage", Name = "Manage Commissions", Description = "Configure commission rules", Module = "Admin" },
            new Permission { Id = "admin.rbac.view", Name = "View RBAC Configuration", Description = "View role-based access control configuration", Module = "Admin" },
            new Permission { Id = "admin.rbac.manage", Name = "Manage RBAC Configuration", Description = "Modify role permissions", Module = "Admin" },

            // Seller Module Permissions
            new Permission { Id = "seller.dashboard.view", Name = "View Seller Dashboard", Description = "Access seller dashboard", Module = "Seller" },
            new Permission { Id = "seller.products.manage", Name = "Manage Products", Description = "Create, edit, and delete products", Module = "Seller" },
            new Permission { Id = "seller.orders.view", Name = "View Seller Orders", Description = "View orders for seller's products", Module = "Seller" },
            new Permission { Id = "seller.orders.fulfill", Name = "Fulfill Orders", Description = "Process and ship orders", Module = "Seller" },
            new Permission { Id = "seller.returns.manage", Name = "Manage Returns", Description = "Process return requests", Module = "Seller" },
            new Permission { Id = "seller.settings.manage", Name = "Manage Seller Settings", Description = "Configure store settings and payout info", Module = "Seller" },
            new Permission { Id = "seller.reports.view", Name = "View Seller Reports", Description = "Access seller analytics and reports", Module = "Seller" },

            // Buyer Module Permissions
            new Permission { Id = "buyer.profile.manage", Name = "Manage Profile", Description = "Edit buyer profile and preferences", Module = "Buyer" },
            new Permission { Id = "buyer.addresses.manage", Name = "Manage Addresses", Description = "Add and manage shipping addresses", Module = "Buyer" },
            new Permission { Id = "buyer.orders.view", Name = "View Orders", Description = "View order history", Module = "Buyer" },
            new Permission { Id = "buyer.orders.place", Name = "Place Orders", Description = "Checkout and place orders", Module = "Buyer" },
            new Permission { Id = "buyer.returns.request", Name = "Request Returns", Description = "Request product returns", Module = "Buyer" },
            new Permission { Id = "buyer.reviews.submit", Name = "Submit Reviews", Description = "Write product and seller reviews", Module = "Buyer" },

            // Product Module Permissions
            new Permission { Id = "product.catalog.view", Name = "View Catalog", Description = "Browse product catalog", Module = "Product" },
            new Permission { Id = "product.details.view", Name = "View Product Details", Description = "View product details", Module = "Product" },
            new Permission { Id = "product.search", Name = "Search Products", Description = "Search and filter products", Module = "Product" },

            // Cart Module Permissions
            new Permission { Id = "cart.manage", Name = "Manage Cart", Description = "Add, remove, and update cart items", Module = "Cart" },
            new Permission { Id = "cart.checkout", Name = "Checkout", Description = "Proceed to checkout", Module = "Cart" },

            // Support/Compliance Module Permissions (for future roles)
            new Permission { Id = "support.cases.view", Name = "View Support Cases", Description = "View customer support cases", Module = "Support" },
            new Permission { Id = "support.cases.manage", Name = "Manage Support Cases", Description = "Handle customer support cases", Module = "Support" },
            new Permission { Id = "compliance.audit.view", Name = "View Audit Logs", Description = "Access audit logs", Module = "Compliance" },
            new Permission { Id = "compliance.data.export", Name = "Export Data", Description = "Export user data for GDPR requests", Module = "Compliance" }
        ];
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Permission>>(_permissions.ToList());
    }

    /// <inheritdoc />
    public Task<Permission?> GetByIdAsync(string permissionId, CancellationToken cancellationToken = default)
    {
        var permission = _permissions.FirstOrDefault(p => p.Id == permissionId);
        return Task.FromResult(permission);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default)
    {
        var permissions = _permissions.Where(p => p.Module.Equals(module, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<Permission>>(permissions);
    }

    /// <inheritdoc />
    public Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        if (!_permissions.Any(p => p.Id == permission.Id))
        {
            _permissions.Add(permission);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        var existing = _permissions.FirstOrDefault(p => p.Id == permission.Id);
        if (existing != null)
        {
            existing.Name = permission.Name;
            existing.Description = permission.Description;
            existing.Module = permission.Module;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string permissionId, CancellationToken cancellationToken = default)
    {
        var permission = _permissions.FirstOrDefault(p => p.Id == permissionId);
        if (permission != null)
        {
            _permissions.Remove(permission);
        }
        return Task.CompletedTask;
    }
}
