using Mercato.Identity.Application.Commands;
using Mercato.Identity.Domain.Entities;
using Mercato.Identity.Domain.Interfaces;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Mercato.Tests.Identity;

public class RbacConfigurationServiceTests
{
    [Fact]
    public async Task GetAllPermissionsAsync_ReturnsAllPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            new() { Id = "admin.users.view", Name = "View Users", Description = "View user accounts", Module = "Admin" },
            new() { Id = "seller.products.manage", Name = "Manage Products", Description = "Manage products", Module = "Seller" }
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        mockPermissionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager();

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.GetAllPermissionsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == "admin.users.view");
        Assert.Contains(result, p => p.Id == "seller.products.manage");
        mockPermissionRepo.VerifyAll();
    }

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllRoles()
    {
        // Arrange
        var roles = new List<IdentityRole>
        {
            new() { Name = "Admin" },
            new() { Name = "Seller" },
            new() { Name = "Buyer" }
        }.AsQueryable();

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager(roles);

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.GetAllRolesAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Admin", result);
        Assert.Contains("Seller", result);
        Assert.Contains("Buyer", result);
    }

    [Fact]
    public async Task GetRoleConfigurationAsync_WithValidRole_ReturnsConfiguration()
    {
        // Arrange
        var roleName = "Admin";
        var permissions = new List<Permission>
        {
            new() { Id = "admin.users.view", Name = "View Users", Description = "View user accounts", Module = "Admin" },
            new() { Id = "admin.users.manage", Name = "Manage Users", Description = "Manage user accounts", Module = "Admin" }
        };
        var rolePermissions = new List<RolePermission>
        {
            new() { RoleName = "Admin", PermissionId = "admin.users.view" }
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        mockPermissionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        mockRolePermissionRepo.Setup(r => r.GetByRoleAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rolePermissions);

        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(r => r.FindByNameAsync(roleName))
            .ReturnsAsync(new IdentityRole(roleName));

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.GetRoleConfigurationAsync(roleName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(roleName, result.RoleName);
        Assert.Equal(2, result.Permissions.Count);
        Assert.Contains(result.Permissions, p => p.Id == "admin.users.view" && p.IsAssigned);
        Assert.Contains(result.Permissions, p => p.Id == "admin.users.manage" && !p.IsAssigned);
        mockPermissionRepo.VerifyAll();
        mockRolePermissionRepo.VerifyAll();
    }

    [Fact]
    public async Task GetRoleConfigurationAsync_WithInvalidRole_ReturnsNull()
    {
        // Arrange
        var roleName = "NonExistentRole";

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(r => r.FindByNameAsync(roleName))
            .ReturnsAsync((IdentityRole?)null);

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.GetRoleConfigurationAsync(roleName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AssignPermissionAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var command = new AssignPermissionCommand
        {
            RoleName = "Admin",
            PermissionId = "admin.users.view",
            AdminUserId = "admin-123"
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        mockPermissionRepo.Setup(r => r.GetByIdAsync(command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Permission { Id = command.PermissionId, Name = "View Users" });

        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        mockRolePermissionRepo.Setup(r => r.HasPermissionAsync(command.RoleName, command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockRolePermissionRepo.Setup(r => r.AddAsync(It.IsAny<RolePermission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(r => r.FindByNameAsync(command.RoleName))
            .ReturnsAsync(new IdentityRole(command.RoleName));

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.AssignPermissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockRolePermissionRepo.Verify(r => r.AddAsync(It.Is<RolePermission>(rp => 
            rp.RoleName == command.RoleName && 
            rp.PermissionId == command.PermissionId &&
            rp.GrantedBy == command.AdminUserId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignPermissionAsync_WithInvalidRole_ReturnsFailure()
    {
        // Arrange
        var command = new AssignPermissionCommand
        {
            RoleName = "NonExistentRole",
            PermissionId = "admin.users.view",
            AdminUserId = "admin-123"
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(r => r.FindByNameAsync(command.RoleName))
            .ReturnsAsync((IdentityRole?)null);

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.AssignPermissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("does not exist"));
    }

    [Fact]
    public async Task AssignPermissionAsync_WithInvalidPermission_ReturnsFailure()
    {
        // Arrange
        var command = new AssignPermissionCommand
        {
            RoleName = "Admin",
            PermissionId = "nonexistent.permission",
            AdminUserId = "admin-123"
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        mockPermissionRepo.Setup(r => r.GetByIdAsync(command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(r => r.FindByNameAsync(command.RoleName))
            .ReturnsAsync(new IdentityRole(command.RoleName));

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.AssignPermissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("does not exist"));
    }

    [Fact]
    public async Task AssignPermissionAsync_WhenAlreadyAssigned_ReturnsFailure()
    {
        // Arrange
        var command = new AssignPermissionCommand
        {
            RoleName = "Admin",
            PermissionId = "admin.users.view",
            AdminUserId = "admin-123"
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        mockPermissionRepo.Setup(r => r.GetByIdAsync(command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Permission { Id = command.PermissionId, Name = "View Users" });

        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        mockRolePermissionRepo.Setup(r => r.HasPermissionAsync(command.RoleName, command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(r => r.FindByNameAsync(command.RoleName))
            .ReturnsAsync(new IdentityRole(command.RoleName));

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.AssignPermissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already assigned"));
    }

    [Fact]
    public async Task AssignPermissionAsync_WithEmptyRoleName_ReturnsValidationError()
    {
        // Arrange
        var command = new AssignPermissionCommand
        {
            RoleName = "",
            PermissionId = "admin.users.view",
            AdminUserId = "admin-123"
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager();

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.AssignPermissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Role name is required"));
    }

    [Fact]
    public async Task RevokePermissionAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var command = new RevokePermissionCommand
        {
            RoleName = "Admin",
            PermissionId = "admin.users.view",
            AdminUserId = "admin-123"
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        mockPermissionRepo.Setup(r => r.GetByIdAsync(command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Permission { Id = command.PermissionId, Name = "View Users" });

        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        mockRolePermissionRepo.Setup(r => r.HasPermissionAsync(command.RoleName, command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockRolePermissionRepo.Setup(r => r.RemoveAsync(command.RoleName, command.PermissionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(r => r.FindByNameAsync(command.RoleName))
            .ReturnsAsync(new IdentityRole(command.RoleName));

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.RevokePermissionAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockRolePermissionRepo.Verify(r => r.RemoveAsync(command.RoleName, command.PermissionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokePermissionAsync_WhenNotAssigned_ReturnsFailure()
    {
        // Arrange
        var command = new RevokePermissionCommand
        {
            RoleName = "Admin",
            PermissionId = "admin.users.view",
            AdminUserId = "admin-123"
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        mockPermissionRepo.Setup(r => r.GetByIdAsync(command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Permission { Id = command.PermissionId, Name = "View Users" });

        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        mockRolePermissionRepo.Setup(r => r.HasPermissionAsync(command.RoleName, command.PermissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(r => r.FindByNameAsync(command.RoleName))
            .ReturnsAsync(new IdentityRole(command.RoleName));

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.RevokePermissionAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("not assigned"));
    }

    [Fact]
    public async Task HasPermissionAsync_WhenPermissionExists_ReturnsTrue()
    {
        // Arrange
        var roleName = "Admin";
        var permissionId = "admin.users.view";

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        mockRolePermissionRepo.Setup(r => r.HasPermissionAsync(roleName, permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockRoleManager = CreateMockRoleManager();

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.HasPermissionAsync(roleName, permissionId);

        // Assert
        Assert.True(result);
        mockRolePermissionRepo.VerifyAll();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenPermissionDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var roleName = "Buyer";
        var permissionId = "admin.users.view";

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        mockRolePermissionRepo.Setup(r => r.HasPermissionAsync(roleName, permissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var mockRoleManager = CreateMockRoleManager();

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.HasPermissionAsync(roleName, permissionId);

        // Assert
        Assert.False(result);
        mockRolePermissionRepo.VerifyAll();
    }

    [Fact]
    public async Task GetModulesAsync_ReturnsDistinctModules()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            new() { Id = "admin.users.view", Name = "View Users", Module = "Admin" },
            new() { Id = "admin.users.manage", Name = "Manage Users", Module = "Admin" },
            new() { Id = "seller.products.manage", Name = "Manage Products", Module = "Seller" },
            new() { Id = "buyer.orders.view", Name = "View Orders", Module = "Buyer" }
        };

        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        mockPermissionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager();

        var service = new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, mockRoleManager.Object);

        // Act
        var result = await service.GetModulesAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Admin", result);
        Assert.Contains("Seller", result);
        Assert.Contains("Buyer", result);
    }

    [Fact]
    public void Constructor_WithNullPermissionRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RbacConfigurationService(null!, mockRolePermissionRepo.Object, mockRoleManager.Object));
    }

    [Fact]
    public void Constructor_WithNullRolePermissionRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var mockRoleManager = CreateMockRoleManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RbacConfigurationService(mockPermissionRepo.Object, null!, mockRoleManager.Object));
    }

    [Fact]
    public void Constructor_WithNullRoleManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockPermissionRepo = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var mockRolePermissionRepo = new Mock<IRolePermissionRepository>(MockBehavior.Strict);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RbacConfigurationService(mockPermissionRepo.Object, mockRolePermissionRepo.Object, null!));
    }

    private static Mock<RoleManager<IdentityRole>> CreateMockRoleManager(IQueryable<IdentityRole>? roles = null)
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        var mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            store.Object, null!, null!, null!, null!);

        roles ??= new List<IdentityRole>
        {
            new() { Name = "Admin" },
            new() { Name = "Seller" },
            new() { Name = "Buyer" }
        }.AsQueryable();

        mockRoleManager.Setup(r => r.Roles).Returns(roles);
        
        return mockRoleManager;
    }
}
