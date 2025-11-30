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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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

        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAccountManagementService(null!, mockAuthEventRepo.Object, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullAuthEventRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAccountManagementService(mockUserManager.Object, null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UserAccountManagementService(mockUserManager.Object, mockAuthEventRepo.Object, null!));
    }

    [Fact]
    public async Task GetUsersAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockAuthEventRepo = new Mock<IAuthenticationEventRepository>();
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
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
        var mockLogger = new Mock<ILogger<UserAccountManagementService>>();

        var service = new UserAccountManagementService(
            mockUserManager.Object,
            mockAuthEventRepo.Object,
            mockLogger.Object);

        var query = new UserAccountFilterQuery { SearchTerm = "abc123" };

        // Act
        var result = await service.GetUsersAsync(query);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("abc123-user", result.Items[0].UserId);
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
