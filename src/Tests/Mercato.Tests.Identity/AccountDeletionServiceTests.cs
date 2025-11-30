using Mercato.Identity.Application.Services;
using Mercato.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Identity;

public class AccountDeletionServiceTests
{
    [Fact]
    public async Task DeleteAccountAsync_WithValidUser_ReturnsSuccess()
    {
        // Arrange
        var userId = "user-123";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>(MockBehavior.Strict);
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockCheckService.Setup(x => x.CheckBlockingConditionsAsync(userId))
            .ReturnsAsync(AccountDeletionCheckResult.CanProceed());
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Buyer" });
        mockUserManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        
        mockDataProvider.Setup(x => x.AnonymizeOrderDataAsync(userId))
            .ReturnsAsync(5);
        mockDataProvider.Setup(x => x.DeleteDeliveryAddressesAsync(userId))
            .ReturnsAsync(2);
        mockDataProvider.Setup(x => x.AnonymizeReviewsAsync(userId))
            .ReturnsAsync(3);
        mockDataProvider.Setup(x => x.AnonymizeStoreDataAsync(userId))
            .ReturnsAsync(false);

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object,
            mockDataProvider.Object);

        // Act
        var result = await service.DeleteAccountAsync(userId, userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.DeletedAt);
        mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
        mockDataProvider.Verify(x => x.AnonymizeOrderDataAsync(userId), Times.Once);
        mockDataProvider.Verify(x => x.DeleteDeliveryAddressesAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithNonExistentUser_ReturnsUserNotFound()
    {
        // Arrange
        var userId = "nonexistent-user";
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((IdentityUser?)null);

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object);

        // Act
        var result = await service.DeleteAccountAsync(userId, userId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsUserNotFound);
        Assert.Contains("User not found.", result.Errors);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithDifferentRequestingUser_ReturnsNotAuthorized()
    {
        // Arrange
        var userId = "user-123";
        var requestingUserId = "different-user";
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object);

        // Act
        var result = await service.DeleteAccountAsync(userId, requestingUserId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
        Assert.Contains("not authorized", result.Errors[0]);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithBlockingConditions_ReturnsBlocked()
    {
        // Arrange
        var userId = "user-123";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var blockingConditions = new List<string> { "You have 2 open disputes" };
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockCheckService.Setup(x => x.CheckBlockingConditionsAsync(userId))
            .ReturnsAsync(AccountDeletionCheckResult.CannotProceed(
                blockingConditions.AsReadOnly(),
                hasOpenDisputes: true,
                openDisputeCount: 2));

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object);

        // Act
        var result = await service.DeleteAccountAsync(userId, userId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsBlocked);
        Assert.Single(result.BlockingConditions);
        Assert.Contains("2 open disputes", result.BlockingConditions[0]);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithExternalLogins_RemovesLogins()
    {
        // Arrange
        var userId = "user-123";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var logins = new List<UserLoginInfo>
        {
            new UserLoginInfo("Google", "google-key", "Google")
        };
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>(MockBehavior.Strict);
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockCheckService.Setup(x => x.CheckBlockingConditionsAsync(userId))
            .ReturnsAsync(AccountDeletionCheckResult.CanProceed());
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);
        mockUserManager.Setup(x => x.RemoveLoginAsync(user, "Google", "google-key"))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());
        mockUserManager.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        
        mockDataProvider.Setup(x => x.AnonymizeOrderDataAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.DeleteDeliveryAddressesAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.AnonymizeReviewsAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.AnonymizeStoreDataAsync(userId))
            .ReturnsAsync(false);

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object,
            mockDataProvider.Object);

        // Act
        var result = await service.DeleteAccountAsync(userId, userId);

        // Assert
        Assert.True(result.Succeeded);
        mockUserManager.Verify(x => x.RemoveLoginAsync(user, "Google", "google-key"), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenDeleteFails_ReturnsFailure()
    {
        // Arrange
        var userId = "user-123";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>(MockBehavior.Strict);
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockCheckService.Setup(x => x.CheckBlockingConditionsAsync(userId))
            .ReturnsAsync(AccountDeletionCheckResult.CanProceed());
        mockUserManager.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());
        mockUserManager.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));
        
        mockDataProvider.Setup(x => x.AnonymizeOrderDataAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.DeleteDeliveryAddressesAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.AnonymizeReviewsAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.AnonymizeStoreDataAsync(userId))
            .ReturnsAsync(false);

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object,
            mockDataProvider.Object);

        // Act
        var result = await service.DeleteAccountAsync(userId, userId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Delete failed", result.Errors[0]);
    }

    [Fact]
    public async Task GetDeletionImpactAsync_WithValidUser_ReturnsImpactInfo()
    {
        // Arrange
        var userId = "user-123";
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>();
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Buyer" });
        
        mockDataProvider.Setup(x => x.GetOrderCountAsync(userId))
            .ReturnsAsync(5);
        mockDataProvider.Setup(x => x.GetDeliveryAddressCountAsync(userId))
            .ReturnsAsync(2);
        mockDataProvider.Setup(x => x.GetReviewCountAsync(userId))
            .ReturnsAsync(3);
        mockDataProvider.Setup(x => x.GetStoreNameAsync(userId))
            .ReturnsAsync((string?)null);

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object,
            mockDataProvider.Object);

        // Act
        var result = await service.GetDeletionImpactAsync(userId);

        // Assert
        Assert.True(result.UserFound);
        Assert.Equal("test@example.com", result.Email);
        Assert.Single(result.Roles);
        Assert.Contains("Buyer", result.Roles);
        Assert.Equal(5, result.OrderCount);
        Assert.Equal(2, result.DeliveryAddressCount);
        Assert.Equal(3, result.ReviewCount);
        Assert.False(result.HasStore);
        mockDataProvider.VerifyAll();
    }

    [Fact]
    public async Task GetDeletionImpactAsync_WithSellerUser_ReturnsStoreInfo()
    {
        // Arrange
        var userId = "seller-123";
        var user = new IdentityUser { Id = userId, Email = "seller@example.com" };
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>();
        var mockDataProvider = new Mock<IAccountDeletionDataProvider>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Seller" });
        
        mockDataProvider.Setup(x => x.GetOrderCountAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.GetDeliveryAddressCountAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.GetReviewCountAsync(userId))
            .ReturnsAsync(0);
        mockDataProvider.Setup(x => x.GetStoreNameAsync(userId))
            .ReturnsAsync("My Store");

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object,
            mockDataProvider.Object);

        // Act
        var result = await service.GetDeletionImpactAsync(userId);

        // Assert
        Assert.True(result.UserFound);
        Assert.True(result.HasStore);
        Assert.Equal("My Store", result.StoreName);
    }

    [Fact]
    public async Task GetDeletionImpactAsync_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = "nonexistent-user";
        
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>();
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((IdentityUser?)null);

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object);

        // Act
        var result = await service.GetDeletionImpactAsync(userId);

        // Assert
        Assert.False(result.UserFound);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>();
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.DeleteAccountAsync(null!, "user-123"));
    }

    [Fact]
    public async Task DeleteAccountAsync_WithNullRequestingUserId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>();
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        var service = new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.DeleteAccountAsync("user-123", null!));
    }

    [Fact]
    public void Constructor_WithNullUserManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockCheckService = new Mock<IAccountDeletionCheckService>();
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AccountDeletionService(
            null!,
            mockCheckService.Object,
            mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullCheckService_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockLogger = new Mock<ILogger<AccountDeletionService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AccountDeletionService(
            mockUserManager.Object,
            null!,
            mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockCheckService = new Mock<IAccountDeletionCheckService>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AccountDeletionService(
            mockUserManager.Object,
            mockCheckService.Object,
            null!));
    }

    private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
