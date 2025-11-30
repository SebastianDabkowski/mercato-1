using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Admin.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Admin;

public class UserAccountManagementServiceTests
{
    private static Mock<IUserBlockRepository> CreateMockUserBlockRepository()
    {
        var mock = new Mock<IUserBlockRepository>();
        mock.Setup(x => x.GetActiveBlockAsync(It.IsAny<string>()))
            .ReturnsAsync((UserBlockInfo?)null);
        mock.Setup(x => x.GetBlockHistoryAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<UserBlockInfo>());
        return mock;
    }

    [Fact]
    public async Task GetUsersAsync_WithNoFilters_ReturnsAllUsers()
    {
        // Arrange
        var user1 = new IdentityUser { Id = "user-1", Email = "buyer@example.com", EmailConfirmed = true };
        var user2 = new IdentityUser { Id = "user-2", Email = "seller@example.com", EmailConfirmed = true };
        var users = new List<IdentityUser> { user1, user2 }.AsQueryable();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.Users).Returns(users);
        mockUserManager.Setup(x => x.GetRolesAsync(user1)).ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetRolesAsync(user2)).ReturnsAsync(new List<string> { "Seller" });

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var query = new UserAccountFilterQuery();

        // Act
        var result = await service.GetUsersAsync(query);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, u => u.UserId == "user-1" && u.Roles.Contains("Buyer"));
        Assert.Contains(result.Items, u => u.UserId == "user-2" && u.Roles.Contains("Seller"));
    }

    [Fact]
    public async Task GetUsersAsync_WithRoleFilter_ReturnsFilteredUsers()
    {
        // Arrange
        var user1 = new IdentityUser { Id = "user-1", Email = "buyer@example.com", EmailConfirmed = true };
        var user2 = new IdentityUser { Id = "user-2", Email = "seller@example.com", EmailConfirmed = true };
        var users = new List<IdentityUser> { user1, user2 }.AsQueryable();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.Users).Returns(users);
        mockUserManager.Setup(x => x.GetRolesAsync(user1)).ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetRolesAsync(user2)).ReturnsAsync(new List<string> { "Seller" });

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var query = new UserAccountFilterQuery { Role = "Seller" };

        // Act
        var result = await service.GetUsersAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("user-2", result.Items[0].UserId);
    }

    [Fact]
    public async Task GetUsersAsync_WithStatusFilter_ReturnsFilteredUsers()
    {
        // Arrange
        var user1 = new IdentityUser { Id = "user-1", Email = "active@example.com", EmailConfirmed = true };
        var user2 = new IdentityUser { Id = "user-2", Email = "pending@example.com", EmailConfirmed = false };
        var users = new List<IdentityUser> { user1, user2 }.AsQueryable();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.Users).Returns(users);
        mockUserManager.Setup(x => x.GetRolesAsync(user1)).ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetRolesAsync(user2)).ReturnsAsync(new List<string> { "Buyer" });

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var query = new UserAccountFilterQuery { Status = UserAccountStatus.PendingVerification };

        // Act
        var result = await service.GetUsersAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("user-2", result.Items[0].UserId);
        Assert.Equal(UserAccountStatus.PendingVerification, result.Items[0].Status);
    }

    [Fact]
    public async Task GetUsersAsync_WithSearchTerm_ReturnsMatchingUsers()
    {
        // Arrange
        var user1 = new IdentityUser { Id = "user-1", Email = "john@example.com", EmailConfirmed = true };
        var user2 = new IdentityUser { Id = "user-2", Email = "jane@example.com", EmailConfirmed = true };
        var users = new List<IdentityUser> { user1, user2 }.AsQueryable();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.Users).Returns(users);
        mockUserManager.Setup(x => x.GetRolesAsync(user1)).ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetRolesAsync(user2)).ReturnsAsync(new List<string> { "Buyer" });

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var query = new UserAccountFilterQuery { SearchTerm = "john" };

        // Act
        var result = await service.GetUsersAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("user-1", result.Items[0].UserId);
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var users = Enumerable.Range(1, 25)
            .Select(i => new IdentityUser
            {
                Id = $"user-{i:D2}",
                Email = $"user{i:D2}@example.com",
                EmailConfirmed = true
            })
            .ToList();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.Users).Returns(users.AsQueryable());
        foreach (var user in users)
        {
            mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Buyer" });
        }

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var query = new UserAccountFilterQuery { Page = 2, PageSize = 10 };

        // Act
        var result = await service.GetUsersAsync(query);

        // Assert
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetUsersAsync_WithBlockedUser_ReturnsBlockedStatus()
    {
        // Arrange
        var lockedUser = new IdentityUser
        {
            Id = "user-1",
            Email = "locked@example.com",
            EmailConfirmed = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddHours(1)
        };
        var users = new List<IdentityUser> { lockedUser }.AsQueryable();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.Users).Returns(users);
        mockUserManager.Setup(x => x.GetRolesAsync(lockedUser)).ReturnsAsync(new List<string> { "Buyer" });

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var query = new UserAccountFilterQuery();

        // Act
        var result = await service.GetUsersAsync(query);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(UserAccountStatus.Blocked, result.Items[0].Status);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithValidUserId_ReturnsUserDetail()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new IdentityUser
        {
            Id = userId,
            Email = "test@example.com",
            EmailConfirmed = true,
            TwoFactorEnabled = true,
            LockoutEnabled = true,
            AccessFailedCount = 2
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Seller" });

        var authEvents = new List<AuthenticationEvent>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                EventType = AuthenticationEventType.Login,
                IsSuccessful = true,
                OccurredAt = DateTimeOffset.UtcNow.AddHours(-1)
            }
        };

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        mockAuthEventRepo.Setup(x => x.GetFilteredAsync(
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<AuthenticationEventType?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(authEvents);

        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetUserDetailAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("test@example.com", result.Email);
        Assert.Contains("Seller", result.Roles);
        Assert.Equal(UserAccountStatus.Active, result.Status);
        Assert.True(result.EmailConfirmed);
        Assert.True(result.TwoFactorEnabled);
        Assert.True(result.LockoutEnabled);
        Assert.Equal(2, result.AccessFailedCount);
        Assert.NotEmpty(result.RecentLoginActivity);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("invalid-id")).ReturnsAsync((IdentityUser?)null);

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetUserDetailAsync("invalid-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithEmptyUserId_ReturnsNull()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetUserDetailAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAccountManagementService(null!, mockAuthEventRepo.Object, mockUserBlockRepo.Object, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullAuthEventRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAccountManagementService(mockUserManager.Object, null!, mockUserBlockRepo.Object, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAccountManagementService(mockUserManager.Object, mockAuthEventRepo.Object, mockUserBlockRepo.Object, null!));
    }

    [Fact]
    public async Task GetUsersAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.GetUsersAsync(null!));
    }

    [Fact]
    public async Task GetUsersAsync_SearchByUserId_ReturnsMatchingUser()
    {
        // Arrange
        var user1 = new IdentityUser { Id = "abc123-user", Email = "john@example.com", EmailConfirmed = true };
        var user2 = new IdentityUser { Id = "xyz789-user", Email = "jane@example.com", EmailConfirmed = true };
        var users = new List<IdentityUser> { user1, user2 }.AsQueryable();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.Users).Returns(users);
        mockUserManager.Setup(x => x.GetRolesAsync(user1)).ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.GetRolesAsync(user2)).ReturnsAsync(new List<string> { "Buyer" });

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var query = new UserAccountFilterQuery { SearchTerm = "abc123" };

        // Act
        var result = await service.GetUsersAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("abc123-user", result.Items[0].UserId);
    }

    [Fact]
    public async Task BlockUserAsync_WithValidCommand_SuccessfullyBlocksUser()
    {
        // Arrange
        var userId = "user-to-block";
        var user = new IdentityUser { Id = userId, Email = "user@example.com", EmailConfirmed = true };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        mockUserManager.Setup(x => x.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(IdentityResult.Success);

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetActiveBlockAsync(userId)).ReturnsAsync((UserBlockInfo?)null);
        mockUserBlockRepo.Setup(x => x.AddAsync(It.IsAny<UserBlockInfo>()))
            .ReturnsAsync((UserBlockInfo b) => b);

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var command = new BlockUserCommand
        {
            UserId = userId,
            AdminUserId = "admin-1",
            AdminEmail = "admin@example.com",
            Reason = BlockReason.Fraud,
            ReasonDetails = "Fraudulent activity detected"
        };

        // Act
        var result = await service.BlockUserAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockUserBlockRepo.Verify(x => x.AddAsync(It.Is<UserBlockInfo>(
            b => b.UserId == userId &&
                 b.BlockedByAdminId == "admin-1" &&
                 b.Reason == BlockReason.Fraud &&
                 b.IsActive == true &&
                 b.BlockedAt > DateTimeOffset.UtcNow.AddMinutes(-1))), Times.Once);
        // Verify lockout is set to a far future date (100 years from now)
        mockUserManager.Verify(x => x.SetLockoutEndDateAsync(user, It.Is<DateTimeOffset?>(
            d => d.HasValue && d.Value > DateTimeOffset.UtcNow.AddYears(50))), Times.Once);
    }

    [Fact]
    public async Task BlockUserAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync("non-existent")).ReturnsAsync((IdentityUser?)null);

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var command = new BlockUserCommand
        {
            UserId = "non-existent",
            AdminUserId = "admin-1",
            AdminEmail = "admin@example.com",
            Reason = BlockReason.Spam
        };

        // Act
        var result = await service.BlockUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User not found.", result.Errors);
    }

    [Fact]
    public async Task BlockUserAsync_WithAlreadyBlockedUser_ReturnsFailure()
    {
        // Arrange
        var userId = "already-blocked";
        var user = new IdentityUser { Id = userId, Email = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetActiveBlockAsync(userId))
            .ReturnsAsync(new UserBlockInfo { UserId = userId, IsActive = true });

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var command = new BlockUserCommand
        {
            UserId = userId,
            AdminUserId = "admin-1",
            AdminEmail = "admin@example.com",
            Reason = BlockReason.PolicyViolation
        };

        // Act
        var result = await service.BlockUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User is already blocked.", result.Errors);
    }

    [Fact]
    public async Task UnblockUserAsync_WithValidCommand_SuccessfullyUnblocksUser()
    {
        // Arrange
        var userId = "user-to-unblock";
        var user = new IdentityUser { Id = userId, Email = "user@example.com" };
        var existingBlock = new UserBlockInfo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlockedByAdminId = "admin-1",
            IsActive = true
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        mockUserManager.Setup(x => x.SetLockoutEndDateAsync(user, null))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetActiveBlockAsync(userId)).ReturnsAsync(existingBlock);
        mockUserBlockRepo.Setup(x => x.UpdateAsync(It.IsAny<UserBlockInfo>())).Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var command = new UnblockUserCommand
        {
            UserId = userId,
            AdminUserId = "admin-2",
            AdminEmail = "admin2@example.com"
        };

        // Act
        var result = await service.UnblockUserAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        mockUserBlockRepo.Verify(x => x.UpdateAsync(It.Is<UserBlockInfo>(
            b => b.IsActive == false &&
                 b.UnblockedByAdminId == "admin-2" &&
                 b.UnblockedAt.HasValue)), Times.Once);
        mockUserManager.Verify(x => x.SetLockoutEndDateAsync(user, null), Times.Once);
    }

    [Fact]
    public async Task UnblockUserAsync_WithNonBlockedUser_ReturnsFailure()
    {
        // Arrange
        var userId = "not-blocked";
        var user = new IdentityUser { Id = userId, Email = "user@example.com" };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetActiveBlockAsync(userId)).ReturnsAsync((UserBlockInfo?)null);

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var command = new UnblockUserCommand
        {
            UserId = userId,
            AdminUserId = "admin-1",
            AdminEmail = "admin@example.com"
        };

        // Act
        var result = await service.UnblockUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User is not currently blocked.", result.Errors);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithBlockedUser_ReturnsBlockInfo()
    {
        // Arrange
        var userId = "blocked-user";
        var user = new IdentityUser
        {
            Id = userId,
            Email = "blocked@example.com",
            EmailConfirmed = true
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Buyer" });

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        mockAuthEventRepo.Setup(x => x.GetFilteredAsync(
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<AuthenticationEventType?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationEvent>());

        var activeBlock = new UserBlockInfo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlockedByAdminId = "admin-1",
            BlockedByAdminEmail = "admin@example.com",
            Reason = BlockReason.Fraud,
            ReasonDetails = "Fraud detected",
            BlockedAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsActive = true
        };

        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetActiveBlockAsync(userId)).ReturnsAsync(activeBlock);
        mockUserBlockRepo.Setup(x => x.GetBlockHistoryAsync(userId)).ReturnsAsync(new List<UserBlockInfo> { activeBlock });

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetUserDetailAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsBlocked);
        Assert.Equal("admin@example.com", result.BlockedByAdminEmail);
        Assert.Equal("Fraud", result.BlockReason);
        Assert.Equal("Fraud detected", result.BlockReasonDetails);
        Assert.NotNull(result.BlockedAt);
    }

    [Fact]
    public async Task GetActiveBlockAsync_WithBlockedUser_ReturnsBlockInfo()
    {
        // Arrange
        var userId = "blocked-user";
        var activeBlock = new UserBlockInfo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsActive = true
        };

        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetActiveBlockAsync(userId)).ReturnsAsync(activeBlock);

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetActiveBlockAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetActiveBlockAsync_WithEmptyUserId_ReturnsNull()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetActiveBlockAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task BlockUserAsync_WithMissingUserId_ReturnsValidationError()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var command = new BlockUserCommand
        {
            UserId = "",
            AdminUserId = "admin-1",
            AdminEmail = "admin@example.com",
            Reason = BlockReason.Spam
        };

        // Act
        var result = await service.BlockUserAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("User ID is required.", result.Errors);
    }

    [Fact]
    public async Task GetBlockHistoryAsync_WithValidUserId_ReturnsHistory()
    {
        // Arrange
        var userId = "user-with-history";
        var blockHistory = new List<UserBlockInfo>
        {
            new UserBlockInfo
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BlockedByAdminId = "admin-1",
                BlockedByAdminEmail = "admin1@example.com",
                Reason = BlockReason.Fraud,
                ReasonDetails = "Fraud detected",
                BlockedAt = DateTimeOffset.UtcNow.AddDays(-10),
                IsActive = false,
                UnblockedAt = DateTimeOffset.UtcNow.AddDays(-5),
                UnblockedByAdminId = "admin-2",
                UnblockedByAdminEmail = "admin2@example.com"
            },
            new UserBlockInfo
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BlockedByAdminId = "admin-3",
                BlockedByAdminEmail = "admin3@example.com",
                Reason = BlockReason.PolicyViolation,
                ReasonDetails = "Repeated policy violations",
                BlockedAt = DateTimeOffset.UtcNow.AddDays(-1),
                IsActive = true
            }
        };

        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetBlockHistoryAsync(userId)).ReturnsAsync(blockHistory);

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetBlockHistoryAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("admin1@example.com", result[0].BlockedByAdminEmail);
        Assert.Equal("Fraud", result[0].Reason);
        Assert.False(result[0].IsActive);
        Assert.NotNull(result[0].UnblockedAt);
        Assert.Equal("admin2@example.com", result[0].UnblockedByAdminEmail);
        Assert.Equal("admin3@example.com", result[1].BlockedByAdminEmail);
        Assert.Equal("PolicyViolation", result[1].Reason);
        Assert.True(result[1].IsActive);
    }

    [Fact]
    public async Task GetBlockHistoryAsync_WithEmptyUserId_ReturnsEmptyList()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = CreateMockUserBlockRepository();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetBlockHistoryAsync("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithBlockHistory_IncludesBlockHistory()
    {
        // Arrange
        var userId = "user-with-history";
        var user = new IdentityUser
        {
            Id = userId,
            Email = "user@example.com",
            EmailConfirmed = true
        };

        var blockHistory = new List<UserBlockInfo>
        {
            new UserBlockInfo
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BlockedByAdminId = "admin-1",
                BlockedByAdminEmail = "admin1@example.com",
                Reason = BlockReason.Spam,
                BlockedAt = DateTimeOffset.UtcNow.AddDays(-10),
                IsActive = false,
                UnblockedAt = DateTimeOffset.UtcNow.AddDays(-5),
                UnblockedByAdminId = "admin-2",
                UnblockedByAdminEmail = "admin2@example.com"
            }
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Buyer" });

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        mockAuthEventRepo.Setup(x => x.GetFilteredAsync(
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<AuthenticationEventType?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuthenticationEvent>());

        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetActiveBlockAsync(userId)).ReturnsAsync((UserBlockInfo?)null);
        mockUserBlockRepo.Setup(x => x.GetBlockHistoryAsync(userId)).ReturnsAsync(blockHistory);

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetUserDetailAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsBlocked);
        Assert.Single(result.BlockHistory);
        Assert.Equal("admin1@example.com", result.BlockHistory[0].BlockedByAdminEmail);
        Assert.Equal("admin2@example.com", result.BlockHistory[0].UnblockedByAdminEmail);
    }

    [Fact]
    public async Task UnblockUserAsync_StoresUnblockedByAdminEmail()
    {
        // Arrange
        var userId = "user-to-unblock";
        var user = new IdentityUser { Id = userId, Email = "user@example.com" };
        var existingBlock = new UserBlockInfo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BlockedByAdminId = "admin-1",
            IsActive = true
        };

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        mockUserManager.Setup(x => x.SetLockoutEndDateAsync(user, null))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockUserBlockRepo = new Mock<IUserBlockRepository>();
        mockUserBlockRepo.Setup(x => x.GetActiveBlockAsync(userId)).ReturnsAsync(existingBlock);
        mockUserBlockRepo.Setup(x => x.UpdateAsync(It.IsAny<UserBlockInfo>())).Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockUserBlockRepo.Object,
            mockLogger.Object);

        var command = new UnblockUserCommand
        {
            UserId = userId,
            AdminUserId = "admin-2",
            AdminEmail = "admin2@example.com"
        };

        // Act
        var result = await service.UnblockUserAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        mockUserBlockRepo.Verify(x => x.UpdateAsync(It.Is<UserBlockInfo>(
            b => b.IsActive == false &&
                 b.UnblockedByAdminId == "admin-2" &&
                 b.UnblockedByAdminEmail == "admin2@example.com" &&
                 b.UnblockedAt.HasValue)), Times.Once);
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
