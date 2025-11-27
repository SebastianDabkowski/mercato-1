using Mercato.Admin.Application.Commands;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class UserRoleManagementServiceTests
{
    [Fact]
    public async Task ChangeUserRoleAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var userId = "test-user-id";
        var adminUserId = "admin-user-id";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, "Seller"))
            .ReturnsAsync(IdentityResult.Success);

        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        mockAuditRepo.Setup(x => x.AddAsync(It.IsAny<RoleChangeAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        var command = new ChangeUserRoleCommand
        {
            UserId = userId,
            NewRole = "Seller",
            AdminUserId = adminUserId
        };

        // Act
        var result = await service.ChangeUserRoleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockUserManager.Verify(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        mockUserManager.Verify(x => x.AddToRoleAsync(user, "Seller"), Times.Once);
        mockAuditRepo.Verify(x => x.AddAsync(It.IsAny<RoleChangeAuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WithInvalidUserId_ReturnsFailure()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("invalid-user-id"))
            .ReturnsAsync((IdentityUser?)null);

        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        var command = new ChangeUserRoleCommand
        {
            UserId = "invalid-user-id",
            NewRole = "Seller",
            AdminUserId = "admin-user-id"
        };

        // Act
        var result = await service.ChangeUserRoleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0]);
        mockAuditRepo.Verify(x => x.AddAsync(It.IsAny<RoleChangeAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WithSameRole_ReturnsFailure()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Buyer" });

        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        var command = new ChangeUserRoleCommand
        {
            UserId = userId,
            NewRole = "Buyer",
            AdminUserId = "admin-user-id"
        };

        // Act
        var result = await service.ChangeUserRoleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("already has the role", result.Errors[0]);
        mockUserManager.Verify(x => x.RemoveFromRolesAsync(It.IsAny<IdentityUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        mockAuditRepo.Verify(x => x.AddAsync(It.IsAny<RoleChangeAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_AdminRemovingOwnAdminRole_ReturnsFailure()
    {
        // Arrange
        var adminUserId = "admin-user-id";
        var adminUser = new IdentityUser { Id = adminUserId, Email = "admin@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(adminUserId))
            .ReturnsAsync(adminUser);
        mockUserManager.Setup(x => x.GetRolesAsync(adminUser))
            .ReturnsAsync(new List<string> { "Admin" });

        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        var command = new ChangeUserRoleCommand
        {
            UserId = adminUserId,
            NewRole = "Buyer",
            AdminUserId = adminUserId // Admin trying to change their own role
        };

        // Act
        var result = await service.ChangeUserRoleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains("cannot remove your own Admin role", result.Errors[0]);
        mockUserManager.Verify(x => x.RemoveFromRolesAsync(It.IsAny<IdentityUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        mockAuditRepo.Verify(x => x.AddAsync(It.IsAny<RoleChangeAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllUsersWithRolesAsync_ReturnsUserList()
    {
        // Arrange
        var user1 = new IdentityUser { Id = "user-1", Email = "user1@example.com" };
        var user2 = new IdentityUser { Id = "user-2", Email = "user2@example.com" };
        var users = new List<IdentityUser> { user1, user2 }.AsQueryable();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.Users).Returns(users);
        mockUserManager.Setup(x => x.GetRolesAsync(user1)).ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetRolesAsync(user2)).ReturnsAsync(new List<string> { "Seller" });

        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetAllUsersWithRolesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.UserId == "user-1" && u.Roles.Contains("Buyer"));
        Assert.Contains(result, u => u.UserId == "user-2" && u.Roles.Contains("Seller"));
    }

    [Fact]
    public async Task GetUserWithRolesAsync_WithValidUserId_ReturnsUserInfo()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin", "Buyer" });

        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetUserWithRolesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal(2, result.Roles.Count);
        Assert.Contains("Admin", result.Roles);
        Assert.Contains("Buyer", result.Roles);
    }

    [Fact]
    public async Task GetUserWithRolesAsync_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("invalid-user-id"))
            .ReturnsAsync((IdentityUser?)null);

        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetUserWithRolesAsync("invalid-user-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WithEmptyUserId_ReturnsFailure()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        var command = new ChangeUserRoleCommand
        {
            UserId = "",
            NewRole = "Seller",
            AdminUserId = "admin-user-id"
        };

        // Act
        var result = await service.ChangeUserRoleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required", result.Errors[0]);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_WithInvalidRole_ReturnsFailure()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        var service = new UserRoleManagementService(
            mockUserManager.Object,
            mockAuditRepo.Object,
            mockLogger.Object);

        var command = new ChangeUserRoleCommand
        {
            UserId = "test-user-id",
            NewRole = "InvalidRole",
            AdminUserId = "admin-user-id"
        };

        // Act
        var result = await service.ChangeUserRoleAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Invalid role", result.Errors[0]);
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserRoleManagementService(null!, mockAuditRepo.Object, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullAuditRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<UserRoleManagementService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserRoleManagementService(mockUserManager.Object, null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuditRepo = new Mock<IRoleChangeAuditRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserRoleManagementService(mockUserManager.Object, mockAuditRepo.Object, null!));
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
